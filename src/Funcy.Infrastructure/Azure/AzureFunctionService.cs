using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.AppService;
using Funcy.Core.Interfaces;
using Funcy.Core.Model;
using Funcy.Data;
using Funcy.Data.Entities;
using Funcy.Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Funcy.Infrastructure.Azure;

public class AzureFunctionService(
    ILogger<AzureFunctionService> logger,
    IAzureSubscriptionService subscriptionService,
    KuduApiClient kuduApiClient,
    IDbContextFactory<FunctionAppDbContext> dbContextFactory) : IAzureFunctionService
{
    private readonly ArmClient _client = new(new DefaultAzureCredential());

    public async IAsyncEnumerable<FunctionAppDetails> FetchFunctionAppDetailsAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var subscriptionId = await subscriptionService.GetCurrentSubscriptionId();
        var subscription = _client.GetSubscriptionResource(new ResourceIdentifier($"/subscriptions/{subscriptionId}"));
        var webSites = subscription.GetWebSitesAsync();
        
        await foreach (var item in GetFunctionAppDetailsAsync(subscriptionId, webSites, cancellationToken))
        {
            yield return item;
        }
    }
    
    public async IAsyncEnumerable<FunctionAppDetails> FetchSpecificFunctionAppDetailsAsync(IEnumerable<FunctionAppDetails> functionAppDetails, [EnumeratorCancellation] CancellationToken cancellationToken)
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

    private async IAsyncEnumerable<FunctionAppDetails> GetFunctionAppDetailsAsync(string subscriptionId,
        IAsyncEnumerable<WebSiteResource> webSites, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var channel = Channel.CreateUnbounded<FunctionAppDetails>();
        var tasks = new List<Task>();
        var throttler = new SemaphoreSlim(5);
        await foreach (var webSite in webSites)
        {
            if (!webSite.Data.Name.StartsWith("func-sp-weeu-ais"))
            {
                continue;
            }
            var task = Task.Run(async () =>
            {
                try
                {
                    await throttler.WaitAsync(cancellationToken);
                    
                    await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

                    var existing = await dbContext.FunctionApps
                        .Include(f => f.Functions)
                        .FirstOrDefaultAsync(f =>
                            f.Name == webSite.Data.Name && f.ResourceGroup == webSite.Data.ResourceGroup &&
                            f.Subscription == subscriptionId, cancellationToken: cancellationToken);

                    List<Function>? functionList = null;
                    try
                    {
                        functionList = await FetchFunctionListFromKudu(webSite);
                    }
                    catch (Exception e)
                    {
                        logger.LogError(e, "Error while fetching function list details {FunctionAppName}",
                            webSite.Data.Name);
                    }
                    
                    var functionApp = AddOrUpdateFunctionApp(existing, webSite, functionList, dbContext);
                    await dbContext.SaveChangesAsync(cancellationToken);
                    await channel.Writer.WriteAsync(functionApp.Map(), cancellationToken);
                }
                catch (Exception e)
                {
                    //TODO: if error when getting function details, perhaps add to a list and then retry? or use polly
                    logger.LogError(e, "Error while fetching function app details {FunctionAppName}",
                        webSite.Data.Name);
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
    }

    private FunctionApp AddOrUpdateFunctionApp(FunctionApp? functionApp, WebSiteResource webSite, List<Function>? functionList, FunctionAppDbContext dbContext)
    {
        if (functionApp is null)
        {
            webSite.Data.Tags.TryGetValue("System", out var systemName);
            functionApp = new FunctionApp()
            {
                Name = webSite.Data.Name,
                State = webSite.Data.State,
                System = systemName ?? string.Empty,
                Subscription = webSite.Id?.SubscriptionId ?? string.Empty,
                ResourceGroup = webSite.Data.ResourceGroup,
                Functions = functionList ?? []
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
        }

        return functionApp;
    }

    private async Task<List<Function>> FetchFunctionListFromKudu(WebSiteResource webSite)
    {
        var kuduInfoList = await kuduApiClient.GetFunctionsAsync(webSite.Data.Name);

        var functionList = kuduInfoList.Select(kuduFunctionInfo => new Function()
            { Name = kuduFunctionInfo.Name, Trigger = kuduFunctionInfo.TriggerType }).ToList();
        return functionList;
    }

    public List<FunctionAppDetails> GetFunctionsFromDatabase()
    {
        using var dbContext = dbContextFactory.CreateDbContext(); 
        var functionAppList = dbContext.FunctionApps.Include(x => x.Functions).Select(x => x.Map()).ToList();
        functionAppList.Sort((a, b) => string.Compare(a.System, b.System, StringComparison.Ordinal));
        return functionAppList;
    }
}