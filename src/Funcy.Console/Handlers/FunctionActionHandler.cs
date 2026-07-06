using System.Collections.Concurrent;
using Funcy.Console.Handlers.Concurrency;
using Funcy.Console.Handlers.Models;
using Funcy.Console.Ui;
using Funcy.Console.Ui.Input;
using Funcy.Console.Ui.State;
using Funcy.Core.Interfaces;
using Funcy.Core.Model;

namespace Funcy.Console.Handlers;

public class FunctionActionHandler(
    IFunctionAppManagementService functionAppManagement,
    IAzureFunctionService functionService,
    FunctionStatusManager functionStatusManager,
    IUiErrorLog uiErrorLog) : IActionDispatcher
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
                    details.State = FunctionState.Running;
                    break;
                case FunctionAction.Stop:
                    await functionAppManagement.StopFunction(details);
                    details.State = FunctionState.Stopped;
                    details.Functions = [];
                    break;
                case FunctionAction.Swap:
                    await functionAppManagement.SwapFunction(details, inputResult.SlotDetails);
                    details = await functionService.GetFunctionAppDetails(details);
                    details.State = FunctionState.Running;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            await functionStatusManager.CompleteOperation(details, inputResult.Action, true);
        }
        catch (Exception ex)
        {
            // Keep the transient row-level Error status; additionally surface it persistently
            // (cancellations are not real failures, so they are not logged).
            await functionStatusManager.CompleteOperation(details, inputResult.Action, false);
            if (ex is not OperationCanceledException)
            {
                uiErrorLog.Report(details.Name, $"{inputResult.Action} failed: {ex.Message}");
            }
        }
        finally
        {
            _currentTasks.TryRemove(inputResult.FunctionAppDetails.Name, out _);
        }
    }
    
    private async Task<FunctionAppDetails> LoadStartedFunctionAppDetails(FunctionAppDetails details)
    {
        const int maxAttempts = 5;
        details.State = FunctionState.Running;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            var updatedDetails = await functionService.GetFunctionAppDetails(details);
            if (updatedDetails.Functions.Count > 0 || attempt == maxAttempts)
            {
                return updatedDetails;
            }

            await Task.Delay(TimeSpan.FromSeconds(attempt));
        }

        return details;
    }
}
