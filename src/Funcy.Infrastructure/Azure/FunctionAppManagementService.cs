using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.AppService;
using Funcy.Core.Interfaces;
using Funcy.Core.Model;
using Funcy.Data;
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
        await UpdateFunctionApp(functionAppDetails, "Running");
        logger.LogInformation("Started Function App: {FunctionAppName}", functionAppDetails.Name);
    }

    public async Task StopFunction(FunctionAppDetails functionAppDetails)
    {
        var webSiteResource = _client.GetWebSiteResource(ResourceIdentifier.Parse(functionAppDetails.Id));
        await webSiteResource.StopAsync();
        await UpdateFunctionApp(functionAppDetails, "Stopped");
        logger.LogInformation("Stopped Function App: {FunctionAppName}", functionAppDetails.Name);
    }

    private async Task UpdateFunctionApp(FunctionAppDetails functionAppDetails, string state)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var existing = await dbContext.FunctionApps
            .Include(f => f.Functions)
            .FirstOrDefaultAsync(f =>
                f.Name == functionAppDetails.Name && f.ResourceGroup == functionAppDetails.ResourceGroup &&
                f.Subscription == functionAppDetails.Subscription);
        
        if (existing is not null)
        {
            existing.State = state;
            await dbContext.SaveChangesAsync();
        }
    }

    public async Task SwapFunction(FunctionAppDetails functionAppDetails)
    {
        throw new NotImplementedException();
    }
}