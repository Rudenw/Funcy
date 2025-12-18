using System.Collections.Concurrent;
using Funcy.Console.Handlers.Concurrency;
using Funcy.Console.Handlers.Models;
using Funcy.Console.Ui.Input;
using Funcy.Core.Interfaces;
using Funcy.Core.Model;

namespace Funcy.Console.Handlers;

public class FunctionActionHandler(
    IFunctionAppManagementService functionAppManagement,
    FunctionStateCoordinator functionStateCoordinator,
    AnimationHandler animationHandler)
{
    private readonly ConcurrentDictionary<string, DispatchedFunction> _currentTasks = [];
    
    public async Task UpdateFunctionAppStatus(FunctionAppDetails functionAppDetails)
    {
        var timeToLive = functionAppDetails.Status.GetTimeToLive();
        if (timeToLive <= 0) return;
        
        await Task.Delay(TimeSpan.FromSeconds(timeToLive));
        functionAppDetails.Status.Status = StatusType.Idle;
        functionAppDetails.Status.Action = null;
        functionAppDetails.LastUpdated = DateTime.UtcNow;
        
        await functionStateCoordinator.PublishUpdateAsync(functionAppDetails);
    }

    private async Task AddNewTask(string name, DispatchedFunction dispatchedFunction)
    {
        _currentTasks.TryAdd(name, dispatchedFunction);
        if (dispatchedFunction.FunctionAppDetails.Status.Status != StatusType.InProgress)
        {
            dispatchedFunction.FunctionAppDetails.Status.Status = StatusType.InProgress;
            dispatchedFunction.FunctionAppDetails.Status.Action = dispatchedFunction.Action;
            dispatchedFunction.FunctionAppDetails.LastUpdated = DateTime.UtcNow;
            
            animationHandler.AddAppDetails(dispatchedFunction.FunctionAppDetails.Key);
            await functionStateCoordinator.PublishUpdateAsync(dispatchedFunction.FunctionAppDetails);
        }
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
                new DispatchedFunction(inputResult.Action, inputResult.FunctionAppDetails));

            _ = dispatchedTask.ContinueWith(async t =>
            {
                if (t.IsCompletedSuccessfully)
                {
                    if (inputResult.Action != FunctionAction.Swap)
                    {
                        inputResult.FunctionAppDetails.State = inputResult.Action.GetFunctionState();                                    
                    }
                                
                    inputResult.FunctionAppDetails.Status.Status = StatusType.Success;
                    inputResult.FunctionAppDetails.Status.Action = null;
                                
                    animationHandler.RemoveAppDetails(inputResult.FunctionAppDetails.Key);
                    await functionStateCoordinator.PublishUpdateAsync(inputResult.FunctionAppDetails);
                }
                else
                {
                    inputResult.FunctionAppDetails.Status.Status = StatusType.Error;
                    inputResult.FunctionAppDetails.Status.Action = null;
                                
                    animationHandler.RemoveAppDetails(inputResult.FunctionAppDetails.Key);
                    await functionStateCoordinator.PublishUpdateAsync(inputResult.FunctionAppDetails);
                }

                _ = UpdateFunctionAppStatus(inputResult.FunctionAppDetails);
                            
                _currentTasks.TryRemove(inputResult.FunctionAppDetails.Name, out _);
            });
        }
    }
}