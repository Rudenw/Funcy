using System.Collections.Concurrent;
using Funcy.Console.Concurrency;
using Funcy.Console.Handlers.Models;
using Funcy.Console.Ui.Input;
using Funcy.Core.Interfaces;
using Funcy.Core.Model;
using Microsoft.Extensions.Logging;

namespace Funcy.Console.Handlers;

public class FunctionActionHandler(
    IFunctionAppManagementService functionAppManagement,
    FunctionStateCoordinator functionStateCoordinator)
{
    private readonly ConcurrentDictionary<string, DispatchedFunction> _currentTasks = [];

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
                                task.FunctionAppDetails.State = task.Action.GetActivatedState();
                                await functionStateCoordinator.PublishUpdateAsync(task.FunctionAppDetails);
                            }
                            else
                            {
                                //TODO: handle errors
                            }
                            
                            _currentTasks.TryRemove(key, out _);
                        }
                    }
                }
            }
        }, token);

        await Task.WhenAll(firstTask);
    }

    private async Task AddNewTask(string name, DispatchedFunction dispatchedFunction)
    {
        _currentTasks.TryAdd(name, dispatchedFunction);
        dispatchedFunction.FunctionAppDetails.State = dispatchedFunction.Action.GetActivatingState();
        await functionStateCoordinator.PublishUpdateAsync(dispatchedFunction.FunctionAppDetails);
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