using System.Collections.Concurrent;
using Funcy.Console.Concurrency;
using Funcy.Core.Interfaces;
using Funcy.Core.Model;

namespace Funcy.Console.Handlers;

public class FunctionAppUpdateHandler
{
    private readonly IAzureFunctionService _functionService;
    private readonly FunctionStateCoordinator _functionStateCoordinator;

    public FunctionAppUpdateHandler(IAzureFunctionService functionService, FunctionStateCoordinator functionStateCoordinator)
    {
        _functionService = functionService;
        _functionStateCoordinator = functionStateCoordinator;
        var functionsFromDatabase = functionService.GetFunctionsFromDatabase();
        _functionStateCoordinator.InitCache(functionsFromDatabase);
    }

    public async Task StartListeningAsync(CancellationToken token)
    {
        await Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                var functionAppDetailsToUpdate = _functionService.FetchFunctionAppDetailsAsync(token);
                await UpdateFunctionAppList(functionAppDetailsToUpdate);
                
                await Task.Delay(TimeSpan.FromMinutes(5), token);
            }
        }, token);
    }

    private async Task UpdateFunctionAppList(IAsyncEnumerable<FunctionAppDetails> functionAppDetailsToUpdate)
    {
        List<string> existingFunctionAppNames = [];
        await foreach (var newApp in functionAppDetailsToUpdate)
        {
            existingFunctionAppNames.Add(newApp.Name);
            await _functionStateCoordinator.PublishUpdateAsync(newApp);
        }

        //TODO: do something to make sure we dont empty the list if we get no functions back
        // var removedFunctions = await _functionStateCoordinator.RemoveFunctions(existingFunctionAppNames);
        // await _functionService.RemoveFunctionsFromDatabase(removedFunctions);
    }
}