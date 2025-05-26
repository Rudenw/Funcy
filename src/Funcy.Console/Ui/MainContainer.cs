using System.Collections.Concurrent;
using Funcy.Console.Concurrency;
using Funcy.Console.Handlers.Models;
using Funcy.Console.Ui.Input;
using Funcy.Console.Ui.Panels;
using Funcy.Core.Model;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Funcy.Console.Ui;

public class MainContainer
{
    private readonly TopPanel _topPanel;
    private readonly FunctionAppPanel _functionListPanel;
    private readonly SearchInputManager _searchInput = new();
    private TaskCompletionSource _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public MainContainer(string subscriptionName,
        List<FunctionAppDetails> functionApps,
        FunctionStateCoordinator functionStateCoordinator)
    {
        _topPanel = new TopPanel(subscriptionName);
        _functionListPanel = new FunctionAppPanel(functionApps);
        
        functionStateCoordinator.OnFunctionAppUpdated += details =>
        {
            UpdatePartialData([details]);
            _tcs.TrySetResult();
        };
        
        functionStateCoordinator.OnFunctionAppRemoved += details =>
        {
            RemoveFunctionApps([details]);
            _tcs.TrySetResult();
        };
    }
    
    public Task WaitForTriggerAsync()
    {
        return _tcs.Task;
    }

    public void ResetTrigger()
    {
        _tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public IRenderable BuildMainLayout()
    {
        return new Rows(_topPanel.Panel, _functionListPanel.Panel);
    }

    public InputActionResult? HandleInput(ConsoleKeyInfo keyInfo)
    {
        var action = _searchInput.HandleInput(keyInfo);

        if (action is not null)
        {
            var selectedFunctionAppDetails = _functionListPanel.GetSelectedFunctionAppDetails();
            return new InputActionResult(action.GetValueOrDefault(), selectedFunctionAppDetails);
        }

        _topPanel.SetSearchText(_searchInput.SearchMarkup);
        _functionListPanel.SetSearchText(_searchInput.SearchText);
        _functionListPanel.HandleInput(keyInfo);

        return null;
    }

    public void UpdateData(List<FunctionAppDetails> functionApps)
    {
        _functionListPanel.UpdateData(functionApps);
    }

    public void HandleResize()
    {
        _functionListPanel.HandleResize();
    }
    
    public void UpdatePartialData(List<FunctionAppDetails> functionApps)
    {
        _functionListPanel.UpdatePartialData(functionApps);
    }

    public void UpdateFunctionsInDispatch(ConcurrentDictionary<string, DispatchedFunction> actionHandlerCurrentTasks)
    {
        var functionAppDetails = actionHandlerCurrentTasks.Values.Select(x => x.FunctionAppDetails with { State = x.Action.GetActivatingState() }).ToList();
        _functionListPanel.UpdatePartialData(functionAppDetails);
    }

    public void RemoveFunctionApps(List<FunctionAppDetails> removed)
    {
        _functionListPanel.RemoveFunctionApps(removed);
    }
}