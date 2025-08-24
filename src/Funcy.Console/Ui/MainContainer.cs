using Funcy.Console.Concurrency;
using Funcy.Console.Handlers;
using Funcy.Console.Ui.Input;
using Funcy.Console.Ui.Pagination;
using Funcy.Console.Ui.PanelLayout.Renderers;
using Funcy.Console.Ui.Panels;
using Funcy.Console.Ui.Panels.GenericTestPanel;
using Funcy.Core.Model;
using Spectre.Console;

namespace Funcy.Console.Ui;

public class MainContainer
{
    private readonly FunctionActionHandler _functionActionHandler;
    private readonly TopPanel _topPanel;
    private readonly ListPanel<FunctionAppDetails> _functionListPanel;
    private readonly SearchInputManager _searchInput = new();
    private TaskCompletionSource _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly ListPanel<FunctionAppSlotDetails> _slotPanel;
    private readonly Stack<IListPanel> _bodyPanelStack = new();
    public readonly Layout MainLayout;
    private bool _searchMode;

    public MainContainer(string subscriptionName,
        List<FunctionAppDetails> functionApps,
        FunctionStateCoordinator functionStateCoordinator,
        FunctionActionHandler functionActionHandler)
    {
        _functionActionHandler = functionActionHandler;
        _topPanel = new TopPanel(subscriptionName);
        _functionListPanel = new ListPanel<FunctionAppDetails>(functionApps, new FunctionAppMatcher(),
            new FunctionAppLayoutRenderer(), "Azure Function Apps");
        _slotPanel = new ListPanel<FunctionAppSlotDetails>([], new FunctionAppSlotMatcher(),
            new FunctionAppSlotLayoutRenderer(), "Azure Function Apps Slots");
        
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
                HandleSwap();
            }
            else
            {
                var selectedFunctionAppDetails = _functionListPanel.GetSelectedItem();
                ArgumentNullException.ThrowIfNull(selectedFunctionAppDetails);
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

    //TODO: betterize
    public enum ViewMode
    {
        FunctionApps,
        Slots
    }
    
    private ViewMode _viewMode { get; set; }
    
    private void HandleSwap()
    {
        if (_viewMode == ViewMode.FunctionApps)
        {
            var selectedFunctionApp = _functionListPanel.GetSelectedItem();
            ArgumentNullException.ThrowIfNull(selectedFunctionApp);
            
            if (selectedFunctionApp.Slots.Count == 1)
            {
                var slotDetails = selectedFunctionApp.Slots[0];
                _ = _functionActionHandler.Dispatch(new InputActionResult(FunctionAction.Swap, selectedFunctionApp, slotDetails));
            }
            else
            {
                _slotPanel.UpdateData(selectedFunctionApp.SlotsExtra);
                _bodyPanelStack.Push(_slotPanel);
                _viewMode = ViewMode.Slots;
                RefreshMainLayout();
            }
        }

        if (_viewMode == ViewMode.Slots)
        {
            var selectedFunctionAppDetails = _functionListPanel.GetSelectedItem();
            ArgumentNullException.ThrowIfNull(selectedFunctionAppDetails);
            
            var selectedSlotDetails = _slotPanel.GetSelectedItem();
            _ = _functionActionHandler.Dispatch(new InputActionResult(FunctionAction.Swap, selectedFunctionAppDetails, selectedSlotDetails));
        }

    }

    public void HandleResize()
    {
        _bodyPanelStack.Peek().HandleResize();
    }

    private void UpdatePartialData(List<FunctionAppDetails> functionApps)
    {
        _functionListPanel.UpdatePartialData(functionApps);
    }

    private void RemoveFunctionApps(List<FunctionAppDetails> removed)
    {
        _functionListPanel.RemoveItems(removed);
    }
}