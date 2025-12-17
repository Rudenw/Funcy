using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.AppService;
using Azure.ResourceManager.AppService.Models;
using Funcy.Core.Interfaces;
using Funcy.Core.Model;
using Funcy.Data;
using Funcy.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Funcy.Infrastructure.Azure;

public class FunctionAppManagementService(ILogger<FunctionAppManagementService> logger, IDbContextFactory<FunctionAppDbContext> dbContextFactory) : IFunctionAppManagementService
{
    private readonly ArmClient _client = new(new DefaultAzureCredential());
    
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

    public async Task SwapFunction(FunctionAppDetails functionAppDetails, FunctionAppSlotDetails functionAppSlot)
    {
        try
        {
            await Task.Yield();
            var webSiteResource = _client.GetWebSiteResource(ResourceIdentifier.Parse(functionAppDetails.Id));
            
            var stagingResource = _client.GetWebSiteSlotResource(ResourceIdentifier.Parse(functionAppSlot.Id));

            //StartSlotAsync doesn't always return and can sit and idle forever. Use non-async method
            stagingResource.StartSlot();
        
            await webSiteResource.SwapSlotWithProductionAsync(WaitUntil.Completed,
                new CsmSlotEntity(functionAppSlot.Name, true));
            
            await stagingResource.StopSlotAsync();
            logger.LogInformation("Swapped Function App: {FunctionAppName}", functionAppDetails.Name);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error while swapping {FunctionAppName}",  functionAppDetails.Name);
            throw;
        }

    }
}