using System.Diagnostics;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.AppService;
using Funcy.Infrastructure.Model;

namespace Funcy.Infrastructure.Azure;

public class AzureFunctionService
{
    private readonly string _subscriptionId;

    //resource group: rg-sp-weeu-ais-workflow-tst
    //subscriptionId: ee691e14-38ba-4613-91bc-2287244a60e7
    private readonly ArmClient _client;
    private readonly List<FunctionAppDetails> _functionAppDetailsList;
    private DateTime _lastCacheTime;
    private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(5);

    public AzureFunctionService(string subscriptionId)
    {
        _subscriptionId = subscriptionId;
        _client = new ArmClient(new DefaultAzureCredential());
        _functionAppDetailsList = [];
        _lastCacheTime = DateTime.MinValue;
    }
    
    public async Task<List<FunctionAppDetails>> InitialLoadFunctionAppsAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        
        var subscription = _client.GetSubscriptionResource(new ResourceIdentifier($"/subscriptions/{_subscriptionId}"));
        var webSitesEnumerator = subscription.GetWebSitesAsync().GetAsyncEnumerator();
        int counter = 0;
        try
        {
            while (await webSitesEnumerator.MoveNextAsync())
            {
                counter++;
                var webSite = webSitesEnumerator.Current;
                if (!webSite.Data.Name.StartsWith("func-sp-weeu-ais"))
                {
                    continue;
                }

                webSite.Data.Tags.TryGetValue("System", out var systemName);
                var functionAppDetails = new FunctionAppDetails()
                {
                    Name = webSite.Data.Name,
                    State = webSite.Data.State,
                    System = systemName ?? string.Empty,
                    ResourceGroup = webSite.Data.ResourceGroup
                };
                
                _functionAppDetailsList.Add(functionAppDetails);
            }
        }
        catch (Exception e)
        {
            System.Console.WriteLine(e);
        }
        finally
        {
            await webSitesEnumerator.DisposeAsync();
        }

        stopwatch.Stop();

        _functionAppDetailsList.Sort((a, b) => string.Compare(a.System, b.System, StringComparison.Ordinal));
        
        return _functionAppDetailsList;
    }
    
    public async Task<List<FunctionAppDetails>> UpdateFunctionAppsAsync(CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();
        var tasks = _functionAppDetailsList.Select(async functionApp =>
        {
            try
            {
                var webSite = _client.GetWebSiteResource(new ResourceIdentifier($"/subscriptions/{_subscriptionId}/resourceGroups/{functionApp.ResourceGroup}/providers/Microsoft.Web/sites/{functionApp.Name}"));
                webSite = await webSite.GetAsync(ct);
                functionApp.State = webSite.Data.State;
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Failed to update Function App {functionApp.Name}: {ex.Message}");
            }
        });

        await Task.WhenAll(tasks);
        
        stopwatch.Stop();

        return _functionAppDetailsList;
    }
}