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
    private SlotPanel? _slotPanel;
    private readonly Stack<IPanelController> _bodyPanelStack = new();
    public readonly Layout MainLayout;

    public MainContainer(string subscriptionName,
        List<FunctionAppDetails> functionApps,
        FunctionStateCoordinator functionStateCoordinator)
    {
        _topPanel = new TopPanel(subscriptionName);
        _functionListPanel = new FunctionAppPanel(functionApps);
        _bodyPanelStack.Push(_functionListPanel);
        
        MainLayout = new Layout("Main Layout")
            .SplitRows(
                new Layout("TopPanel").Size(4),
                new Layout("BodyPanel")
            );

        RefreshMainLayout();
        
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

    public void RefreshMainLayout()
    {
        MainLayout["TopPanel"].Update(_topPanel.Panel);
        MainLayout["BodyPanel"].Update(_bodyPanelStack.Peek().Panel);
    }

    public InputActionResult? HandleInput(ConsoleKeyInfo keyInfo)
    {
        var action = _searchInput.HandleInput(keyInfo);

        if (action is not null)
        {
            if (action == FunctionAction.Swap)
            {
                var selectedFunctionAppDetails = _functionListPanel.GetSelectedFunctionAppDetails();
                _slotPanel = new SlotPanel(selectedFunctionAppDetails.Slots);
                _bodyPanelStack.Push(_slotPanel);
                RefreshMainLayout();
                return null;
            }
            else
            {
                var selectedFunctionAppDetails = _functionListPanel.GetSelectedFunctionAppDetails();
                return new InputActionResult(action.GetValueOrDefault(), selectedFunctionAppDetails);
            }
        }

        _topPanel.SetSearchText(_searchInput.SearchMarkup);
        _functionListPanel.SetSearchText(_searchInput.SearchText);
        _functionListPanel.HandleInput(keyInfo);

        return null;
    }

    public void HandleResize()
    {
        _functionListPanel.HandleResize();
    }
    
    public void UpdatePartialData(List<FunctionAppDetails> functionApps)
    {
        _functionListPanel.UpdatePartialData(functionApps);
    }

    public void RemoveFunctionApps(List<FunctionAppDetails> removed)
    {
        _functionListPanel.RemoveFunctionApps(removed);
    }
}