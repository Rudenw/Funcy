using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Channels;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.AppService;
using Funcy.Core.Interfaces;
using Funcy.Core.Model;
using Funcy.Data;
using Funcy.Data.Entities;
using Funcy.Infrastructure.Azure.Models;
using Funcy.Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Funcy.Infrastructure.Azure;

public class AzureFunctionService(
    ILogger<AzureFunctionService> logger,
    IAzureSubscriptionService subscriptionService,
    IDbContextFactory<FunctionAppDbContext> dbContextFactory) : IAzureFunctionService
{
    private readonly ArmClient _client = new(new DefaultAzureCredential());

    public List<FunctionAppDetails> GetFunctionsFromDatabase()
    {
        using var dbContext = dbContextFactory.CreateDbContext(); 
        var functionAppList = dbContext.FunctionApps.Include(x => x.Functions).Select(x => x.Map()).ToList();
        functionAppList.Sort((a, b) => string.Compare(a.System, b.System, StringComparison.Ordinal));
        return functionAppList;
    }
    
    public async IAsyncEnumerable<FunctionAppFetchResult> FetchFunctionAppDetailsAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var subscriptionId = await subscriptionService.GetCurrentSubscriptionId();
        var subscription = _client.GetSubscriptionResource(new ResourceIdentifier($"/subscriptions/{subscriptionId}"));
        var webSites = subscription.GetWebSitesAsync(cancellationToken: cancellationToken);
        
        await foreach (var item in GetFunctionAppDetailsAsync(subscriptionId, webSites, cancellationToken))
        {
            yield return item;
        }
    }
    
    public async IAsyncEnumerable<FunctionAppFetchResult> FetchSpecificFunctionAppDetailsAsync(IEnumerable<FunctionAppDetails> functionAppDetails, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var webSites = functionAppDetails.Select(d => _client.GetWebSiteResource(ResourceIdentifier.Parse(d.Id)).Get().Value);
        
        var subscriptionId = await subscriptionService.GetCurrentSubscriptionId();
        await foreach (var item in GetFunctionAppDetailsAsync(subscriptionId, webSites.ToAsyncEnumerable(), cancellationToken))
        {
            yield return item;
        }
    }

    public async Task RemoveFunctionsFromDatabase(IEnumerable<FunctionAppDetails> removedFunctionApps)
    {
        var dbContext = await dbContextFactory.CreateDbContextAsync();
        foreach (var functionApp in removedFunctionApps)
        {
            var functionAppEntity = await dbContext.FunctionApps.FirstOrDefaultAsync(x => x.Name == functionApp.Name && x.ResourceGroup == functionApp.ResourceGroup && x.Subscription == functionApp.Subscription);
            if (functionAppEntity is not null)
            {
                dbContext.FunctionApps.Remove(functionAppEntity);
            }
        }
        
        await dbContext.SaveChangesAsync();
    }

    private async IAsyncEnumerable<FunctionAppFetchResult> GetFunctionAppDetailsAsync(string subscriptionId,
        IAsyncEnumerable<WebSiteResource> webSites, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        var channel = Channel.CreateUnbounded<FunctionAppFetchResult>();
        var tasks = new List<Task>();
        var throttler = new SemaphoreSlim(5);
        await foreach (var webSite in webSites.WithCancellation(cancellationToken))
        {
            var task = Task.Run(async () =>
            {
                try
                {
                    var functionAppStopwatch = new Stopwatch();
                    await throttler.WaitAsync(cancellationToken);
                    functionAppStopwatch.Start();
                    
                    await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

                    var existing = await dbContext.FunctionApps
                        .Include(f => f.Functions)
                        .Include(f => f.Slots)
                        .FirstOrDefaultAsync(f =>
                            f.Name == webSite.Data.Name && f.ResourceGroup == webSite.Data.ResourceGroup &&
                            f.Subscription == subscriptionId, cancellationToken: cancellationToken);

                    List<Function>? functionList = FetchFunctionListAsync(webSite);
                    List<FunctionAppSlot>? slotList = FetchSlotListAsync(webSite);

                    var functionApp = AddOrUpdateFunctionApp(existing, webSite, functionList, slotList, dbContext);
                    await dbContext.SaveChangesAsync(cancellationToken);
                    await channel.Writer.WriteAsync(new FunctionAppFetchResult(webSite.Id.Name, functionApp.Map()), cancellationToken);
                    functionAppStopwatch.Stop();
                    
                    logger.LogInformation("Processed {FunctionAppName} in {ElapsedMilliseconds}ms", webSite.Data.Name, functionAppStopwatch.ElapsedMilliseconds);
                    
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Error while fetching function app details {FunctionAppName}",
                        webSite.Data.Name);
                    await channel.Writer.WriteAsync(new FunctionAppFetchResult(webSite.Id.Name, null, e.Message), cancellationToken);
                }
                finally
                {
                    throttler.Release();
                }
            }, cancellationToken);
            
            tasks.Add(task);
        }

        _ = Task.Run(async () =>
        {
            await Task.WhenAll(tasks);
            channel.Writer.Complete();
            throttler.Dispose();
        }, cancellationToken);
        
        while (await channel.Reader.WaitToReadAsync(cancellationToken))
        {
            while (channel.Reader.TryRead(out var item))
            {
                yield return item;
            }
        }
        
        stopwatch.Stop();
        logger.LogInformation("Fetched all function app details in {ElapsedMilliseconds}ms", stopwatch.ElapsedMilliseconds);
    }

    private List<Function>? FetchFunctionListAsync(WebSiteResource webSite)
    {
        if (webSite.Data.State.Equals("Stopped", StringComparison.OrdinalIgnoreCase))
            return null;

        List<Function>? functionList = [];
        try
        {
            var sw = Stopwatch.StartNew();
            var websiteFunctions = webSite.GetSiteFunctions();
            foreach (var websiteFunction in websiteFunctions)
            {
                var config = websiteFunction.Data.Config.ToObjectFromJson<FunctionConfig>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                var trigger = "";
                if (config is not null)
                {
                    var triggerBinding = config.Bindings.FirstOrDefault(b =>
                        b.Type.EndsWith("Trigger", StringComparison.OrdinalIgnoreCase) &&
                        (b.Direction == null || b.Direction.Equals("in", StringComparison.OrdinalIgnoreCase)));
                    trigger = triggerBinding?.Type ?? "";
                }
                functionList.Add(new Function { AzureId = websiteFunction.Id.ToString(), Name = websiteFunction.Id.Name, Trigger = trigger });
            }
            sw.Stop();
            logger.LogInformation("Fetched function list for {FunctionAppName} in {ElapsedMilliseconds}ms", webSite.Data.Name, sw.ElapsedMilliseconds);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error while fetching function list details {FunctionAppName}", webSite.Data.Name);
            functionList = null;
        }
        return functionList;
    }
    
    private List<FunctionAppSlot>? FetchSlotListAsync(WebSiteResource webSite)
    {
        List<FunctionAppSlot>? slotList = null;
        try
        {
            var sw = Stopwatch.StartNew();
            var slots = webSite.GetWebSiteSlots();
            slotList = Enumerable.Select(slots,
                slot => new FunctionAppSlot { FullName = slot.Data.Name, Name = slot.Id.Name, AzureId = slot.Id.ToString(), State = slot.Data.State }).ToList();
            sw.Stop();
            logger.LogInformation("Fetched slot list for {FunctionAppName} in {ElapsedMilliseconds}ms", webSite.Data.Name, sw.ElapsedMilliseconds);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error while fetching slot list details {FunctionAppName}", webSite.Data.Name);
        }
        return slotList;
    }

    private FunctionApp AddOrUpdateFunctionApp(FunctionApp? functionApp, WebSiteResource webSite,
        List<Function>? functionList, List<FunctionAppSlot>? slotList, FunctionAppDbContext dbContext)
    {
        if (functionApp is null)
        {
            webSite.Data.Tags.TryGetValue("System", out var systemName);
            functionApp = new FunctionApp()
            {
                AzureId = webSite.Id.ToString(),
                Name = webSite.Data.Name,
                State = webSite.Data.State,
                System = systemName ?? string.Empty,
                Subscription = webSite.Id?.SubscriptionId ?? string.Empty,
                ResourceGroup = webSite.Data.ResourceGroup,
                Functions = functionList ?? [],
                Slots = slotList ?? []
            };
            dbContext.FunctionApps.Add(functionApp);
        }
        else
        {
            functionApp.State = webSite.Data.State;

            if (functionList is not null)
            {
                dbContext.Functions.RemoveRange(functionApp.Functions);
                functionApp.Functions = functionList;                
            }

            if (slotList is not null)
            {
                dbContext.FunctionAppSlots.RemoveRange(functionApp.Slots);
                functionApp.Slots = slotList;
            }
        }

        return functionApp;
    }
}