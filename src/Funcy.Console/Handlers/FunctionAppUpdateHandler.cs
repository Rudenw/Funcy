using Funcy.Console.Concurrency;
using Funcy.Console.Handlers.Models;
using Funcy.Core.Interfaces;
using Funcy.Core.Model;

namespace Funcy.Console.Handlers;

public class FunctionAppUpdateHandler(
    IAzureFunctionService functionService,
    FunctionStateCoordinator functionStateCoordinator)
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
            while (!token.IsCancellationRequested)
            {
                var functionAppDetailsToUpdate = functionService.FetchFunctionAppDetailsAsync(token);
                await UpdateFunctionAppList(functionAppDetailsToUpdate);
                
                await Task.Delay(TimeSpan.FromMinutes(5), token);
            }
        }, token);
    }

    private async Task UpdateFunctionAppList(IAsyncEnumerable<FunctionAppFetchResult> functionAppDetailsToUpdate)
    {
        List<string> existingFunctionAppNames = [];
        await foreach (var newApp in functionAppDetailsToUpdate)
        {
            existingFunctionAppNames.Add(newApp.Name);
            if (newApp.IsSuccess)
            {
                await functionStateCoordinator.PublishUpdateAsync(CreateFunctionAppUpdate(newApp.Details!));                
            }
        }
        
        var removedFunctions = await functionStateCoordinator.RemoveFunctions(existingFunctionAppNames);
        await functionService.RemoveFunctionsFromDatabase(removedFunctions);
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