using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.AppService;
using Funcy.Infrastructure.Model;

namespace Funcy.Infrastructure.Azure;

public class AzureFunctionService
{
    //resource group: rg-sp-weeu-ais-workflow-tst
    //subscriptionId: ee691e14-38ba-4613-91bc-2287244a60e7
    private readonly ArmClient _client;
    private readonly List<FunctionAppDetails> _cachedFunctions;
    private DateTime _lastCacheTime;
    private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(5);

    public AzureFunctionService()
    {
        _client = new ArmClient(new DefaultAzureCredential());
        _cachedFunctions = [];
        _lastCacheTime = DateTime.MinValue;
    }
    
    public async Task ListFunctionAppsAsync(string subscriptionId)
    {
        var subscription = _client.GetSubscriptionResource(new ResourceIdentifier($"/subscriptions/{subscriptionId}"));

        var webSitesEnumerator = subscription.GetWebSitesAsync().GetAsyncEnumerator();
        try
        {
            while (await webSitesEnumerator.MoveNextAsync())
            {
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
                };

                foreach (var siteFunction in webSite.GetSiteFunctions())
                {
                    var functionDetails = new FunctionDetails
                    {
                        Name = siteFunction.Id.Name,
                    };
                    functionAppDetails.Functions.Add(functionDetails);
                }

                _cachedFunctions.Add(functionAppDetails);
            }
        }
        finally
        {
            await webSitesEnumerator.DisposeAsync();
        }
    }
}