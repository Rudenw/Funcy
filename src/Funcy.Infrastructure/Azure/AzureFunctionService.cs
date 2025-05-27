using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.AppService;
using Funcy.Core.Interfaces;
using Funcy.Core.Model;
using Funcy.Infrastructure.Mappers;
using Funcy.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;

namespace Funcy.Infrastructure.Azure;

public class AzureFunctionService(
    ILogger<AzureFunctionService> logger,
    IAzureSubscriptionService subscriptionService,
    KuduApiClient kuduApiClient,
    IFunctionAppRepository repository) : IAzureFunctionService
{
    private readonly ArmClient _client = new(new DefaultAzureCredential());
    private readonly IFunctionAppRepository _repository = repository;

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
        foreach (var functionApp in removedFunctionApps)
        {
            await _repository.RemoveAsync(functionApp, CancellationToken.None);
        }
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

                    List<FunctionDetails>? functionList = null;
                    try
                    {
                        functionList = await FetchFunctionListFromKudu(webSite);
                    }
                    catch (Exception e)
                    {
                        logger.LogError(e, "Error while fetching function list details {FunctionAppName}",
                            webSite.Data.Name);
                    }

                    webSite.Data.Tags.TryGetValue("System", out var systemName);
                    var details = new FunctionAppDetails
                    {
                        Name = webSite.Data.Name,
                        State = webSite.Data.State,
                        System = systemName ?? string.Empty,
                        Subscription = subscriptionId,
                        ResourceGroup = webSite.Data.ResourceGroup,
                        Functions = functionList ?? []
                    };

                    var updated = await _repository.UpsertAsync(details, functionList, cancellationToken);
                    await channel.Writer.WriteAsync(updated, cancellationToken);
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

    private async Task<List<FunctionDetails>> FetchFunctionListFromKudu(WebSiteResource webSite)
    {
        var kuduInfoList = await kuduApiClient.GetFunctionsAsync(webSite.Data.Name);
        var functionList = kuduInfoList.Select(kuduFunctionInfo => new FunctionDetails
            { Name = kuduFunctionInfo.Name, Trigger = kuduFunctionInfo.TriggerType }).ToList();
        return functionList;
    }

    public List<FunctionAppDetails> GetFunctionsFromDatabase()
    {
        var functionAppList = _repository.GetAllAsync(CancellationToken.None).GetAwaiter().GetResult();
        functionAppList.Sort((a, b) => string.Compare(a.System, b.System, StringComparison.Ordinal));
        return functionAppList;
    }
}