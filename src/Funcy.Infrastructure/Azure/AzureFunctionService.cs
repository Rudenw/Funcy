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
using Funcy.Infrastructure.Azure.Models;
using Funcy.Infrastructure.Mappers;
using Microsoft.Extensions.Logging;

namespace Funcy.Infrastructure.Azure;

public class AzureFunctionService(
    ILogger<AzureFunctionService> logger,
    IAzureSubscriptionService subscriptionService) : IAzureFunctionService
{
    private readonly ArmClient _client = new(new DefaultAzureCredential());
    
    public async Task<IEnumerable<FunctionAppDetails>> GetFunctionAppBaseAsync()
    {
        var sw = Stopwatch.StartNew();
        var allFunctionApps = await subscriptionService.GetAllFunctionApps();
        sw.Stop();
        logger.LogInformation("Fetched all function apps in {ElapsedMilliseconds}ms", sw.ElapsedMilliseconds);
        return allFunctionApps.Select(functionApp => functionApp.Map());
    }
    
    public async IAsyncEnumerable<FunctionAppFetchResult> GetFunctionAppWithAllDataAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        var channel = Channel.CreateUnbounded<FunctionAppFetchResult>();
        var allFunctionApps = await subscriptionService.GetAllFunctionApps();
        var throttler = new SemaphoreSlim(5);
        var tasks = new List<Task>();

        _ = Task.Run(async () =>
        {
            try
            {
                foreach (var functionAppGraphRow in allFunctionApps)
                {
                    await throttler.WaitAsync(cancellationToken);
                    var detailsTask = Task.Run(async () =>
                    {
                        try
                        {
                            var webSiteResource = _client.GetWebSiteResource(ResourceIdentifier.Parse(functionAppGraphRow.Id));
                            var functionTask = Task.Run(() =>
                                GetFunctionAppFunctionsAsync(webSiteResource, functionAppGraphRow.Name), cancellationToken);
                            var slotTask = Task.Run(() => GetFunctionAppSlotsAsync(webSiteResource, functionAppGraphRow.Name), cancellationToken);
                            var functionAppDetails = functionAppGraphRow.Map();

                            await Task.WhenAll(functionTask, slotTask);
                            functionAppDetails.Functions = functionTask.Result ?? [];
                            functionAppDetails.Slots = slotTask.Result ?? [];

                            await channel.Writer.WriteAsync(new FunctionAppFetchResult(functionAppDetails.Name,
                                functionAppDetails), cancellationToken);
                        }
                        catch (Exception e)
                        {
                            await channel.Writer.WriteAsync(
                                new FunctionAppFetchResult(functionAppGraphRow.Name, null, e.Message),
                                cancellationToken);
                        }
                        finally
                        {
                            throttler.Release();
                        }
                    }, cancellationToken);
                    tasks.Add(detailsTask);
                }
                
                await Task.WhenAll(tasks);
            }
            finally
            {
                channel.Writer.Complete();
                throttler.Dispose();
            }
        }, cancellationToken);
        
        sw.Stop();
        
        await foreach (var item in channel.Reader.ReadAllAsync(cancellationToken))
        {
            yield return item;
        }
    }
    
    public async Task<FunctionAppDetails> GetFunctionAppDetails(FunctionAppDetails functionAppDetails)
    {
        if (functionAppDetails.State == FunctionState.Stopped)
        {
            return functionAppDetails;
        }
        
        var sw = Stopwatch.StartNew();
        var webSiteResource = _client.GetWebSiteResource(ResourceIdentifier.Parse(functionAppDetails.Id));
        
        var functionTask = Task.Run(() => GetFunctionAppFunctionsAsync(webSiteResource, functionAppDetails.Name));
        var slotTask = Task.Run(() => GetFunctionAppSlotsAsync(webSiteResource, functionAppDetails.Name));
        await Task.WhenAll(functionTask, slotTask);

        functionAppDetails.Functions = functionTask.Result ?? [];
        functionAppDetails.Slots = slotTask.Result  ?? [];
        functionAppDetails.DetailsLoaded = true;
        
        sw.Stop();
        logger.LogInformation("Fetched all function app details in {ElapsedMilliseconds}ms", sw.ElapsedMilliseconds);

        return functionAppDetails;
    }

    private List<FunctionDetails>? GetFunctionAppFunctionsAsync(WebSiteResource webSite, string functionAppName)
    {
        List<FunctionDetails>? functionList = [];
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
                functionList.Add(new FunctionDetails { FunctionAppName = functionAppName, Name = websiteFunction.Id.Name, Trigger = trigger });
            }
            sw.Stop();
            logger.LogInformation("Fetched function list for {FunctionAppName} in {ElapsedMilliseconds}ms", functionAppName, sw.ElapsedMilliseconds);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error while fetching function list details {FunctionAppName}", functionAppName);
            functionList = null;
        }
        
        return functionList;
    }
    
    private List<FunctionAppSlotDetails>? GetFunctionAppSlotsAsync(WebSiteResource webSite, string functionAppName)
    {
        List<FunctionAppSlotDetails>? slotList = null;
        try
        {
            var sw = Stopwatch.StartNew();
            var slots = webSite.GetWebSiteSlots();
            slotList = Enumerable.Select(slots,
                slot => new FunctionAppSlotDetails { FullName = slot.Data.Name, Name = slot.Id.Name, Id = slot.Id.ToString(), State = Enum.Parse<FunctionState>(slot.Data.State) }).ToList();
            sw.Stop();
            logger.LogInformation("Fetched slot list for {FunctionAppName} in {ElapsedMilliseconds}ms", functionAppName, sw.ElapsedMilliseconds);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error while fetching slot list details {FunctionAppName}", functionAppName);
        }
        return slotList;
    }
}