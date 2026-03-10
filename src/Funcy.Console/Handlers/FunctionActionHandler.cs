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
    IAzureFunctionService functionService,
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
            await AddNewTask(inputResult.FunctionAppDetails.Name,
                new DispatchedFunction(inputResult.Action, inputResult.FunctionAppDetails));

            _ = ExecuteActionAsync(inputResult);
        }
    }

    private async Task ExecuteActionAsync(InputActionResult inputResult)
    {
        var details = inputResult.FunctionAppDetails;

        try
        {
            switch (inputResult.Action)
            {
                case FunctionAction.Start:
                    await functionAppManagement.StartFunction(details);
                    details = await functionService.GetFunctionAppDetails(details);
                    break;
                case FunctionAction.Stop:
                    await functionAppManagement.StopFunction(details);
                    details.State = FunctionState.Stopped;
                    details.Functions = [];
                    break;
                case FunctionAction.Swap:
                    await functionAppManagement.SwapFunction(details, inputResult.SlotDetails);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            await functionStatusManager.CompleteOperation(details, inputResult.Action, true);
        }
        catch
        {
            await functionStatusManager.CompleteOperation(details, inputResult.Action, false);
        }
        finally
        {
            _currentTasks.TryRemove(inputResult.FunctionAppDetails.Name, out _);
        }
    }
}
