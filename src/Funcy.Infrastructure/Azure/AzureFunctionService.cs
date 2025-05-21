using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.AppService;
using Azure.ResourceManager.AppService.Models;
using Funcy.Data;
using Funcy.Data.Entities;
using Funcy.Infrastructure.Mappers;
using Funcy.Infrastructure.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Funcy.Infrastructure.Azure;

public class AzureFunctionService(ILogger<AzureFunctionService> logger, IAzureSubscriptionService subscriptionService, KuduApiClient kuduApiClient, IDbContextFactory<FunctionAppDbContext> dbContextFactory)
{
    private readonly ArmClient _client = new(new DefaultAzureCredential());

    public async IAsyncEnumerable<FunctionAppDetails> FetchFunctionAppDetailsAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var subscriptionId = await subscriptionService.GetCurrentSubscriptionId();
        var subscription = _client.GetSubscriptionResource(new ResourceIdentifier($"/subscriptions/{subscriptionId}"));

        var channel = Channel.CreateUnbounded<FunctionAppDetails>();
        var tasks = new List<Task>();
        var throttler = new SemaphoreSlim(5);
        await foreach (var webSite in subscription.GetWebSitesAsync())
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

                    var functionList = await FetchFunctionListFromKudu(webSite);
                    var functionApp = AddOrUpdateFunctionApp(existing, webSite, functionList, dbContext);
                    await dbContext.SaveChangesAsync(cancellationToken);
                    await channel.Writer.WriteAsync(functionApp.Map(), cancellationToken);
                }
                catch (Exception e)
                {
                    //TODO: if error when getting function details, perhaps add to a list and then retry? or use polly
                    logger.LogError(e, "Error while fetching function app details");;
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

    private FunctionApp AddOrUpdateFunctionApp(FunctionApp? functionApp, WebSiteResource webSite, List<Function> functionList, FunctionAppDbContext dbContext)
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
                Functions = functionList
            };
            dbContext.FunctionApps.Add(functionApp);
        }
        else
        {
            functionApp.State = webSite.Data.State;
            dbContext.Functions.RemoveRange(functionApp.Functions);
            functionApp.Functions = functionList;
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

    public async Task StartFunction(FunctionAppDetails functionAppDetails)
    {
        var webSiteResource = _client.GetWebSiteResource(ResourceIdentifier.Parse(functionAppDetails.Id));
        await webSiteResource.StartAsync();
        logger.LogInformation("Started Function App: {FunctionAppName}", functionAppDetails.Name);
    }

    public async Task StopFunction(FunctionAppDetails functionAppDetails)
    {
        var webSiteResource = _client.GetWebSiteResource(ResourceIdentifier.Parse(functionAppDetails.Id));
        await webSiteResource.StopAsync();
        logger.LogInformation("Stopped Function App: {FunctionAppName}", functionAppDetails.Name);
    }

    public async Task SwapFunction(FunctionAppDetails functionAppDetails)
    {
        throw new NotImplementedException();
    }
}