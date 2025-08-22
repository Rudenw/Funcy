using System.Collections.Concurrent;
using Funcy.Console.Concurrency;
using Funcy.Console.Handlers;
using Funcy.Console.Handlers.Models;
using Funcy.Console.Ui.Input;
using Funcy.Console.Ui.Panels;
using Funcy.Core.Model;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Funcy.Console.Ui;

public class MainContainer
{
    private readonly FunctionActionHandler _functionActionHandler;
    private readonly TopPanel _topPanel;
    private readonly FunctionAppPanel _functionListPanel;
    private readonly SearchInputManager _searchInput = new();
    private TaskCompletionSource _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly SlotPanel _slotPanel;
    private readonly Stack<IBodyPanelController> _bodyPanelStack = new();
    public readonly Layout MainLayout;
    private bool _searchMode;

    public MainContainer(string subscriptionName,
        List<FunctionAppDetails> functionApps,
        FunctionStateCoordinator functionStateCoordinator,
        FunctionActionHandler functionActionHandler)
    {
        _functionActionHandler = functionActionHandler;
        _topPanel = new TopPanel(subscriptionName);
        _functionListPanel = new FunctionAppPanel(functionApps);
        _slotPanel = new SlotPanel([]);
        
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
        
        _functionListPanel.OnSwap += () =>
        {
            var selectedFunctionApp = _functionListPanel.GetSelectedFunctionAppDetails();
            if (selectedFunctionApp.Slots.Count == 1)
            {
                var slotDetails = selectedFunctionApp.Slots[0];
                _ = _functionActionHandler.Dispatch(new InputActionResult(FunctionAction.Swap, selectedFunctionApp, slotDetails));
            }
            else
            {
                _slotPanel.UpdateData(selectedFunctionApp.SlotsExtra);
                _bodyPanelStack.Push(_slotPanel);
                RefreshMainLayout();
            }
        };
        
        _slotPanel.OnSwap += () =>
        {
            var selectedFunctionAppDetails = _functionListPanel.GetSelectedFunctionAppDetails();
            var selectedSlotDetails = _slotPanel.GetSelectedSlot();
            _ = _functionActionHandler.Dispatch(new InputActionResult(FunctionAction.Swap, selectedFunctionAppDetails, selectedSlotDetails));
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

    private void RefreshMainLayout()
    {
        MainLayout["TopPanel"].Update(_topPanel.Panel);
        MainLayout["BodyPanel"].Update(_bodyPanelStack.Peek().Panel);
    }

    public void HandleInput(ConsoleKeyInfo keyInfo)
    {
        FunctionAction? action = null;
        if (_searchMode)
        {
            _searchMode = _searchInput.HandleInput(keyInfo);
        }
        else
        {
            switch (keyInfo.Key)
            {
                case var key when key == Shortcuts.Filter.Key:
                    _searchMode = true;
                    _searchInput.InitializeSearchMode();
                    break;
                case var key when key == Shortcuts.Start.Key:
                    action =  FunctionAction.Start;
                    break;
                case var key when key == Shortcuts.Stop.Key:
                    action =  FunctionAction.Stop;
                    break;
                case var key when key == Shortcuts.Swap.Key:
                    action =  FunctionAction.Swap;
                    break;
                case ConsoleKey.Delete:
                    _searchInput.ClearSearchText();
                    break;
            }
        }
        
        _topPanel.SetSearchText(_searchInput.SearchMarkup);
        _functionListPanel.SetSearchText(_searchInput.SearchText);

        if (action is not null)
        {
            if (action == FunctionAction.Swap)
            {
                _bodyPanelStack.Peek().SwapFunction();
            }
            else
            {
                var selectedFunctionAppDetails = _functionListPanel.GetSelectedFunctionAppDetails();
                _ = _functionActionHandler.Dispatch(new InputActionResult(action.GetValueOrDefault(), selectedFunctionAppDetails));
            }
        }

        if (keyInfo.Key is ConsoleKey.Escape or ConsoleKey.Spacebar && _bodyPanelStack.Count > 1)
        {
            _bodyPanelStack.Pop();
            RefreshMainLayout();
        }
        
        _bodyPanelStack.Peek().HandleInput(keyInfo);
    }

    public void HandleResize()
    {
        _functionListPanel.HandleResize();
    }

    private void UpdatePartialData(List<FunctionAppDetails> functionApps)
    {
        _functionListPanel.UpdatePartialData(functionApps);
    }

    private void RemoveFunctionApps(List<FunctionAppDetails> removed)
    {
        _functionListPanel.RemoveFunctionApps(removed);
    }
}