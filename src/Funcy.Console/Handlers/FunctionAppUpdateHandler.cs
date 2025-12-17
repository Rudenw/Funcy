using System.Collections.Concurrent;
using System.Diagnostics;
using Funcy.Console.Handlers.Concurrency;
using Funcy.Console.Handlers.Models;
using Funcy.Core.Interfaces;
using Funcy.Core.Model;
using Microsoft.Extensions.Logging;

namespace Funcy.Console.Handlers;

public class FunctionAppUpdateHandler(
    ILogger<FunctionAppUpdateHandler> logger,
    IAzureFunctionService functionService,
    FunctionStateCoordinator functionStateCoordinator,
    AnimationHandler animationHandler)
{
    private ConcurrentDictionary<string, Task> _inFlight = new();

    private ConcurrentDictionary<string, FunctionAppDetails> _detailsCache = new();
    
    public async Task InitializeAsync(CancellationToken token)
    {
        // var functionsFromDatabase = await functionService.GetFunctionsFromDatabase(token);
        // functionStateCoordinator.InitCache(functionsFromDatabase);
    }

    public async Task StartListeningAsync(CancellationToken token)
    {
        await Task.Run(async () =>
        {
            var functionAppDetailsToUpdate = await functionService.GetFunctionAppBaseAsync();
            await UpdateFunctionAppList(functionAppDetailsToUpdate);
            
            //Just do a full refresh when the app starts
        }, token);
    }
    
    public void LoadDetails(string currentKey, bool userInitiated)
    {
        if (_inFlight.ContainsKey(currentKey))
        {
            animationHandler.AddAppDetails(currentKey);
            return;
        }
        
        var functionAppDetails = functionStateCoordinator.TryGet(currentKey);
        if (functionAppDetails is not null && (!functionAppDetails.DetailsLoaded || userInitiated))
        {
            if (userInitiated)
            {
                animationHandler.AddAppDetails(currentKey);            
            }
            var loadingTask = Task.Run(async () =>
            {
                var functionAppFromAzure = await functionService.GetFunctionAppDetails(functionAppDetails);
                await functionStateCoordinator.PublishUpdateAsync(CreateFunctionAppUpdate(functionAppFromAzure));
            });
            _inFlight.TryAdd(currentKey, loadingTask);
            loadingTask.ContinueWith(t =>
            {
                _inFlight.TryRemove(currentKey, out _);
                animationHandler.RemoveAppDetails(functionAppDetails.Key);
            });
        }
    }

    private async Task UpdateFunctionAppList(IEnumerable<FunctionAppDetails> functionAppDetailsToUpdate)
    {
        var sw = Stopwatch.StartNew();
        foreach (var newApp in functionAppDetailsToUpdate)
        {
            await functionStateCoordinator.PublishUpdateAsync(CreateFunctionAppUpdate(newApp));
        }
        sw.Stop();
        logger.LogInformation("Updated Function App List in {ElapsedMilliseconds}ms", sw.ElapsedMilliseconds);
    }
    
    private static FunctionAppUpdate CreateFunctionAppUpdate(FunctionAppDetails functionAppDetails)
    {
        return new FunctionAppUpdate()
        {
            FunctionAppDetails = functionAppDetails,
            Source = UpdateSource.Database
        };
    }
}