using System.Collections.Concurrent;
using Funcy.Console.Handlers.Models;
using Funcy.Console.Ui.Input;
using Funcy.Core.Interfaces;
using Funcy.Core.Model;
using Microsoft.Extensions.Logging;

namespace Funcy.Console.Handlers;

public class FunctionActionHandler(IAzureFunctionService functionService, IFunctionAppManagementService functionAppManagement)
{
    private TaskCompletionSource _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public bool IsTriggered { get; set; }
    private readonly ConcurrentDictionary<string, DispatchedFunction> _currentTasks = [];
    private readonly ConcurrentDictionary<string, DispatchedFunction> _completedTasks = [];

    public readonly ConcurrentDictionary<string, DispatchedFunction> UncompletedTasks = [];
    public readonly List<FunctionAppDetails> FunctionApps = [];

    public async Task StartListeningAsync(CancellationToken token)
    {
        var firstTask = Task.Run(() =>
        {
            while (!token.IsCancellationRequested)
            {
                foreach (var key in _currentTasks.Keys.ToList())
                {
                    if (_currentTasks.TryGetValue(key, out var task))
                    {
                        if (task.RunningTask.IsCompleted)
                        {
                            _completedTasks.TryAdd(key, task);
                            _currentTasks.TryRemove(key, out _);
                        }
                        else
                        {
                            if (UncompletedTasks.TryAdd(key, task)) //true = New task
                            {
                                IsTriggered = true;
                                _tcs.TrySetResult();
                            }
                        }
                    }
                }
            }
        }, token);

        var secondTask = Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                if (!_completedTasks.IsEmpty)
                {
                    var functionAppDetails =
                        functionService.FetchSpecificFunctionAppDetailsAsync(
                            _completedTasks.Values.Select(x => x.FunctionAppDetails), token);

                    await foreach (var functionAppDetail in functionAppDetails)
                    {
                        FunctionApps.Add(functionAppDetail);
                    }

                    IsTriggered = true;
                    _tcs.TrySetResult();
                    _completedTasks.Clear();
                }
            }
        }, token);

        await Task.WhenAll(firstTask, secondTask);
    }
    
    public List<FunctionAppDetails> GetAndClearFunctionApps()
    {
        var snapshot = FunctionApps.ToList();
        FunctionApps.Clear();
        return snapshot;
    }

    public Task WaitForTriggerAsync()
    {
        return _tcs.Task;
    }
    
    public void ResetTrigger()
    {
        IsTriggered = false;
        _tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    }
    
    public void Dispatch(InputActionResult inputResult)
    {
        switch (inputResult.Action)
        {
            case FunctionAction.Start:
                if (!_currentTasks.ContainsKey(inputResult.FunctionAppDetails.Name))
                {
                    _currentTasks.TryAdd(inputResult.FunctionAppDetails.Name,
                        new DispatchedFunction(FunctionAction.Start, inputResult.FunctionAppDetails,
                            functionAppManagement.StartFunction(inputResult.FunctionAppDetails)));
                }
                break;
            case FunctionAction.Stop:
                if (!_currentTasks.ContainsKey(inputResult.FunctionAppDetails.Name))
                {
                    _currentTasks.TryAdd(inputResult.FunctionAppDetails.Name,
                        new DispatchedFunction(FunctionAction.Stop, inputResult.FunctionAppDetails,
                            functionAppManagement.StopFunction(inputResult.FunctionAppDetails)));
                }
                break;
            case FunctionAction.Swap:
                if (!_currentTasks.ContainsKey(inputResult.FunctionAppDetails.Name))
                {
                    _currentTasks.TryAdd(inputResult.FunctionAppDetails.Name,
                        new DispatchedFunction(FunctionAction.Swap, inputResult.FunctionAppDetails,
                            functionAppManagement.SwapFunction(inputResult.FunctionAppDetails)));
                }
                break;
        }
    }
}