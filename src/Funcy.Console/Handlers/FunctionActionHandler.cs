using System.Collections.Concurrent;
using Funcy.Console.Handlers.Concurrency;
using Funcy.Console.Handlers.Models;
using Funcy.Console.Ui;
using Funcy.Console.Ui.Input;
using Funcy.Core.Interfaces;
using Funcy.Core.Model;

namespace Funcy.Console.Handlers;

public class FunctionActionHandler(
    IFunctionAppManagementService functionAppManagement,
    FunctionStatusManager functionStatusManager) : IActionDispatcher
{
    private readonly ConcurrentDictionary<string, DispatchedFunction> _currentTasks = [];

    private async Task AddNewTask(string name, DispatchedFunction dispatchedFunction)
    {
        _currentTasks.TryAdd(name, dispatchedFunction);
        if (dispatchedFunction.FunctionAppDetails.Status.Status != StatusType.InProgress)
        {
            await functionStatusManager.BeginOperation(dispatchedFunction.FunctionAppDetails, dispatchedFunction.Action);
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
                if (inputResult.Action != FunctionAction.Swap)
                {
                    inputResult.FunctionAppDetails.State = inputResult.Action.GetFunctionState();                                    
                }

                await functionStatusManager.CompleteOperation(inputResult.FunctionAppDetails, inputResult.Action,
                    t.IsCompletedSuccessfully);
                            
                _currentTasks.TryRemove(inputResult.FunctionAppDetails.Name, out _);
            });
        }
    }
}