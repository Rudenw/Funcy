using System.Collections.Concurrent;
using System.Diagnostics;
using Funcy.Console.Handlers.Concurrency;
using Funcy.Console.Handlers.Models;
using Funcy.Console.Ui;
using Funcy.Core.Interfaces;
using Funcy.Core.Model;
using Microsoft.Extensions.Logging;

namespace Funcy.Console.Handlers;

public class FunctionAppUpdateHandler(
    ILogger<FunctionAppUpdateHandler> logger,
    IAzureFunctionService functionService,
    FunctionStateCoordinator functionStateCoordinator,
    AnimationHandler animationHandler,
    IUiStatusState uiStatusState,
    FunctionStatusManager functionStatusManager)
{
    public async Task InitializeAsync(CancellationToken token)
    {
        var functionsFromDatabase = await functionService.GetFunctionsFromDatabase(token);
        functionStateCoordinator.InitCache(functionsFromDatabase);
    }

    public async Task StartListeningAsync(CancellationToken token)
    {
        await Task.Run(async () =>
        {
            animationHandler.AddAppDetails("TopPanel");
            uiStatusState.BeginInventoryValidation();
            var functionAppDetailsToUpdate = functionService.GetFunctionAppDetailsAsync(token);
            await UpdateFunctionAppList(functionAppDetailsToUpdate);
            uiStatusState.EndInventoryValidation();
            animationHandler.RemoveAppDetails("TopPanel");
            
            uiStatusState.BeginDetailsRefresh();
            await LoadAllDetailsInBackground(token);
            uiStatusState.EndDetailsRefresh();
        }, token);
    }
    
    private async Task LoadAllDetailsInBackground(CancellationToken token)
    {
        var allApps = functionStateCoordinator.GetCachedFunctionAppDetails();
        uiStatusState.SetTotalDetails(allApps.Count);
        var updatedFunctionApps = functionService.GetFunctionAppFunctionsAndSlotsAsync(allApps, token);
        await UpdateFunctionAppList(updatedFunctionApps);
    }
    
    public void LoadDetails(string currentKey)
    {
        var functionAppDetails = functionStateCoordinator.TryGet(currentKey);
        if (functionAppDetails is null) return;
        
        _ = Task.Run(async () =>
        {
            await functionStatusManager.BeginOperation(functionAppDetails, FunctionAction.Refresh);
            var updatedDetails = await functionService.GetFunctionAppDetails(functionAppDetails);
            await functionStatusManager.CompleteOperation(updatedDetails, FunctionAction.Refresh, true);
        });
    }

    private async Task UpdateFunctionAppList(IAsyncEnumerable<FunctionAppFetchResult> functionAppDetailsToUpdate)
    {
        List<string> existingFunctionAppNames = [];
        var sw = Stopwatch.StartNew();
        await foreach (var newApp in functionAppDetailsToUpdate)
        {
            existingFunctionAppNames.Add(newApp.Name);
            if (newApp.IsSuccess)
            {
                await functionStateCoordinator.PublishUpdateAsync(newApp.Details!);
                uiStatusState.IncrementDetailsInFlight();
            }
        }
        uiStatusState.ResetDetailsInFlight();
        sw.Stop();
        logger.LogInformation("Updated Function App List in {ElapsedMilliseconds}ms", sw.ElapsedMilliseconds);
        
        await functionStateCoordinator.RemoveFunctions(existingFunctionAppNames);
    }
}