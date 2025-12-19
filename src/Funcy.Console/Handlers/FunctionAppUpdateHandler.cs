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
    IUiStatusState uiStatusState)
{
    private ConcurrentDictionary<string, Task> _inFlight = new();
    
    
    public async Task InitializeAsync(CancellationToken token)
    {
        var functionsFromDatabase = await functionService.GetFunctionsFromDatabase(token);
        functionStateCoordinator.InitCache(functionsFromDatabase);
    }

    public async Task StartListeningAsync(CancellationToken token)
    {
        await Task.Run(async () =>
        {
            uiStatusState.BeginInventoryValidation();
            var functionAppDetailsToUpdate = functionService.GetFunctionAppDetailsAsync(token);
            await UpdateFunctionAppList(functionAppDetailsToUpdate);
            uiStatusState.EndInventoryValidation();
            
            uiStatusState.BeginDetailsRefresh();
            await LoadAllDetailsInBackground(token);
            uiStatusState.EndDetailsRefresh();
            //Just do a full refresh when the app starts
        }, token);
    }
    
    private async Task LoadAllDetailsInBackground(CancellationToken token)
    {
        var allApps = functionStateCoordinator.GetInitialLoad();
        uiStatusState.SetTotalDetails(allApps.Count);
        var updatedFunctionApps = functionService.GetFunctionAppFunctionsAndSlotsAsync(allApps, token);
        await UpdateFunctionAppList(updatedFunctionApps);
    }
    
    public void LoadDetails(string currentKey, bool userInitiated)
    {
        if (_inFlight.ContainsKey(currentKey))
        {
            animationHandler.AddAppDetails(currentKey);
            return;
        }
        
        var functionAppDetails = functionStateCoordinator.TryGet(currentKey);
        if (functionAppDetails is not null && userInitiated)
        {
            if (userInitiated)
            {
                animationHandler.AddAppDetails(currentKey);            
            }
            var loadingTask = Task.Run(async () =>
            {
                var functionAppFromAzure = await functionService.GetFunctionAppDetails(functionAppDetails);
                await functionStateCoordinator.PublishUpdateAsync(functionAppFromAzure);
            });
            _inFlight.TryAdd(currentKey, loadingTask);
            loadingTask.ContinueWith(t =>
            {
                _inFlight.TryRemove(currentKey, out _);
                animationHandler.RemoveAppDetails(functionAppDetails.Key);
            });
        }
    }

    private async Task UpdateFunctionAppList(IAsyncEnumerable<FunctionAppFetchResult> functionAppDetailsToUpdate)
    {
        var sw = Stopwatch.StartNew();
        await foreach (var newApp in functionAppDetailsToUpdate)
        {
            await functionStateCoordinator.PublishUpdateAsync(newApp.Details!);
            uiStatusState.IncrementDetailsInFlight();
        }
        uiStatusState.ResetDetailsInFlight();
        sw.Stop();
        logger.LogInformation("Updated Function App List in {ElapsedMilliseconds}ms", sw.ElapsedMilliseconds);
    }
}