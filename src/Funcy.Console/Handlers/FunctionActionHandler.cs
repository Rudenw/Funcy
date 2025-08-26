using System.Collections.Concurrent;
using System.Diagnostics;
using Funcy.Console.Concurrency;
using Funcy.Console.Handlers.Models;
using Funcy.Console.Ui.Input;
using Funcy.Core.Interfaces;
using Funcy.Core.Model;

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
                                if (task.Action != FunctionAction.Swap)
                                {
                                    task.FunctionAppDetails.State = task.Action.GetFunctionState();                                    
                                }
                                
                                task.FunctionAppDetails.Status.Status = StatusType.Success;
                                task.FunctionAppDetails.Status.Action = null;
                                
                                await functionStateCoordinator.PublishUpdateAsync(CreateFunctionAppUpdate(task.FunctionAppDetails));
                            }
                            else
                            {
                                task.FunctionAppDetails.Status.Status = StatusType.Error;
                                task.FunctionAppDetails.Status.Action = null;
                                
                                await functionStateCoordinator.PublishUpdateAsync(CreateFunctionAppUpdate(task.FunctionAppDetails));
                            }

                            _ = UpdateFunctionAppStatus(task.FunctionAppDetails);
                            
                            _currentTasks.TryRemove(key, out _);
                        }
                    }
                }
            }
        }, token);

        await Task.WhenAll(firstTask);
    }
    
    public async Task UpdateFunctionAppStatus(FunctionAppDetails functionAppDetails)
    {
        var timeToLive = functionAppDetails.Status.GetTimeToLive();
        if (timeToLive <= 0) return;
        
        await Task.Delay(TimeSpan.FromSeconds(timeToLive));
        functionAppDetails.Status.Status = StatusType.Idle;
        functionAppDetails.Status.Action = null;
        
        await functionStateCoordinator.PublishUpdateAsync(CreateFunctionAppUpdate(functionAppDetails));
    }

    private async Task AddNewTask(string name, DispatchedFunction dispatchedFunction)
    {
        _currentTasks.TryAdd(name, dispatchedFunction);
        if (dispatchedFunction.FunctionAppDetails.Status.Status != StatusType.InProgress)
        {
            dispatchedFunction.FunctionAppDetails.Status.Status = StatusType.InProgress;
            dispatchedFunction.FunctionAppDetails.Status.Action = dispatchedFunction.Action;
            
            await functionStateCoordinator.PublishUpdateAsync(CreateFunctionAppUpdate(dispatchedFunction.FunctionAppDetails));
        }
    }

    private static FunctionAppUpdate CreateFunctionAppUpdate(FunctionAppDetails functionAppDetails)
    {
        return new FunctionAppUpdate
        {
            FunctionAppDetails = functionAppDetails,
            Source = UpdateSource.Action
        };
    }
    
    public async Task Dispatch(InputActionResult inputResult)
    {
        if (!_currentTasks.ContainsKey(inputResult.FunctionAppDetails.Name))
        {
            var dispatchedTask = inputResult.Action switch
            {
                FunctionAction.Start => functionAppManagement.StartFunction(inputResult.FunctionAppDetails),
                FunctionAction.Stop => functionAppManagement.StopFunction(inputResult.FunctionAppDetails),
                FunctionAction.Swap => functionAppManagement.SwapFunction(inputResult.FunctionAppDetails, inputResult.SlotDetails),
                _ => null!
            };
            
            await AddNewTask(inputResult.FunctionAppDetails.Name,
                new DispatchedFunction(inputResult.Action, inputResult.FunctionAppDetails, dispatchedTask));
        }
    }
}