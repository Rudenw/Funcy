using System.Collections.Concurrent;
using Funcy.Console.Concurrency;
using Funcy.Console.Handlers.Models;
using Funcy.Console.Ui.Input;
using Funcy.Core.Interfaces;
using Funcy.Core.Model;
using Microsoft.Extensions.Logging;

namespace Funcy.Console.Handlers;

public class FunctionActionHandler(
    IAzureFunctionService functionService,
    IFunctionAppManagementService functionAppManagement,
    FunctionStateCoordinator functionStateCoordinator)
{
    private readonly ConcurrentDictionary<string, DispatchedFunction> _currentTasks = [];
    private readonly ConcurrentDictionary<string, DispatchedFunction> _completedTasks = [];

    public readonly ConcurrentDictionary<string, DispatchedFunction> UncompletedTasks = [];
    public readonly List<FunctionAppDetails> FunctionApps = [];

    public async Task StartListeningAsync(CancellationToken token)
    {
        var firstTask = Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                foreach (var key in _currentTasks.Keys.ToList())
                {
                    if (_currentTasks.TryGetValue(key, out var task))
                    {
                        if (task.RunningTask.IsCompleted)
                        {
                            if (task.RunningTask.IsCompletedSuccessfully)
                            {
                                await functionStateCoordinator.PublishUpdateAsync(task.FunctionAppDetails with {State = task.Action.GetActivatedState()});
                                // _currentTasks.TryRemove(key, out _);
                            }
                            else
                            {
                                //TODO: handle errors
                            }
                            
                            // _completedTasks.TryAdd(key, task);
                            _currentTasks.TryRemove(key, out _);
                        }
                    }
                }
            }
        }, token);

        // var secondTask = Task.Run(async () =>
        // {
        //     while (!token.IsCancellationRequested)
        //     {
        //         if (!_completedTasks.IsEmpty)
        //         {
        //             var current = _completedTasks.ToArray();
        //             var functionAppDetails =
        //                 functionService.FetchSpecificFunctionAppDetailsAsync(
        //                     current.Select(x => x.Value.FunctionAppDetails), token);
        //
        //             await foreach (var functionAppDetail in functionAppDetails)
        //             {
        //                 await functionStateCoordinator.PublishUpdateAsync(functionAppDetail);
        //             }
        //             
        //             foreach (var item in current)
        //             {
        //                 _completedTasks.TryRemove(item.Key, out _);
        //             }
        //         }
        //     }
        // }, token);

        await Task.WhenAll(firstTask);
    }

    private async Task AddNewTask(string name, DispatchedFunction dispatchedFunction)
    {
        _currentTasks.TryAdd(name, dispatchedFunction);
        await functionStateCoordinator.PublishUpdateAsync(dispatchedFunction.FunctionAppDetails with {State = dispatchedFunction.Action.GetActivatingState()});
    }
    
    public async Task Dispatch(InputActionResult inputResult)
    {
        Task dispatchedTask = inputResult.Action switch
        {
            FunctionAction.Start => functionAppManagement.StartFunction(inputResult.FunctionAppDetails),
            FunctionAction.Stop => functionAppManagement.StopFunction(inputResult.FunctionAppDetails),
            FunctionAction.Swap => functionAppManagement.SwapFunction(inputResult.FunctionAppDetails),
            _ => null!
        };

        if (!_currentTasks.ContainsKey(inputResult.FunctionAppDetails.Name))
        {
            await AddNewTask(inputResult.FunctionAppDetails.Name,
                new DispatchedFunction(inputResult.Action, inputResult.FunctionAppDetails, dispatchedTask));
        }
    }
}