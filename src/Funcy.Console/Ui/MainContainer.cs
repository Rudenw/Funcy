using Funcy.Console.Handlers;
using Funcy.Console.Handlers.Concurrency;
using Funcy.Console.Ui.Contexts;
using Funcy.Console.Ui.Factory;
using Funcy.Console.Ui.Input;
using Funcy.Console.Ui.Panels;
using Funcy.Console.Ui.Panels.Interfaces;
using Funcy.Core.Model;
using Spectre.Console;

namespace Funcy.Console.Ui;

public sealed class MainContainer : IDisposable
{
    private readonly FunctionActionHandler _functionActionHandler;
    private readonly TopPanel _topPanel;
    private TaskCompletionSource _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    
    private readonly Stack<ListPanelContext> _contextStack = new();
    
    public readonly Layout MainLayout;
    private bool _searchMode;
    private readonly ListPanelContextFactory _listPanelContextFactory;
    
    private ListPanelContext Current => _contextStack.Peek();

    public MainContainer(string subscriptionName,
        List<FunctionAppDetails> functionApps,
        FunctionStateCoordinator functionStateCoordinator,
        FunctionActionHandler functionActionHandler)
    {
        _functionActionHandler = functionActionHandler;
        _topPanel = new TopPanel(subscriptionName);

        _listPanelContextFactory = new ListPanelContextFactory(functionStateCoordinator, () => _tcs.TrySetResult());

        var context = _listPanelContextFactory.CreateRoot(functionApps);
        _contextStack.Push(context);
        
        MainLayout = new Layout("Main Layout")
            .SplitRows(
                new Layout("TopPanel").Size(4),
                new Layout("BodyPanel")
            );

        RefreshMainLayout();
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
        UpdateShortcuts();
        SyncSearchUi();

        MainLayout["TopPanel"].Update(_topPanel.Panel);
        MainLayout["BodyPanel"].Update(Current.View.Panel);
    }

    private void UpdateShortcuts()
    {
        var shortcuts = Current.View.GetShortcuts();
        _topPanel.UpdateShortcuts(shortcuts);
    }

    public void HandleInput(ConsoleKeyInfo keyInfo)
    {
        FunctionAction? action = null;
        if (_searchMode)
        {
            _searchMode = Current.SearchInputManager.HandleInput(keyInfo);
            SyncSearchUi();
            return;
        }
        
        switch (keyInfo.Key)
        {
            case var key when key == ListPanelShortcuts.Filter.Key:
                EnterSearchMode();
                break;

            case var key when 
                key == ListPanelShortcuts.Start.Key ||
                key == ListPanelShortcuts.Stop.Key ||
                key == ListPanelShortcuts.Swap.Key:
                HandleActionKey(keyInfo.Key);
                break;

            case ConsoleKey.Delete:
                Current.SearchInputManager.ClearSearchText();
                SyncSearchUi();
                break;

            case ConsoleKey.Enter:
                TryPushNextPanelFromSelection();
                break;

            case ConsoleKey.Escape:
            case ConsoleKey.Spacebar:
                TryPopPanel();
                break;

            case ConsoleKey.UpArrow:
            case ConsoleKey.DownArrow:
                Current.View.HandleInput(keyInfo);
                UpdateShortcuts();
                break;

            default:
                Current.View.HandleInput(keyInfo);
                break;
        }
    }
    
    private void SyncSearchUi()
    {
        _topPanel.SetSearchText(Current.SearchInputManager.SearchMarkup);
        Current.View.SetSearchText(Current.SearchInputManager.SearchText);
    }
    
    private void EnterSearchMode()
    {
        _searchMode = true;
        Current.SearchInputManager.InitializeSearchMode();
        SyncSearchUi();
    }
    
    private void HandleActionKey(ConsoleKey key)
    {
        var action =
            key == ListPanelShortcuts.Start.Key ? FunctionAction.Start :
            key == ListPanelShortcuts.Stop.Key ? FunctionAction.Stop :
            FunctionAction.Swap;

        HandleAction(action);
    }

    private void HandleAction(FunctionAction action)
    {
        var currentView = Current.View;

        if (currentView is IActionHandlingPanel actionPanel &&
            actionPanel.TryBuildAction(action, out var input))
        {
            _ = _functionActionHandler.Dispatch(input);
            return;
        }

        if (currentView.TryGetActionNavigationRequest(out var navRequest) && navRequest is not null)
        {
            var nextContext = _listPanelContextFactory.CreateFromNavigation(navRequest);
            _contextStack.Push(nextContext);
            RefreshMainLayout();
        }
    }
    
    private void TryPopPanel()
    {
        if (_contextStack.Count <= 1)
            return;

        _contextStack.Pop();
        RefreshMainLayout();
    }

    private void TryPushNextPanelFromSelection()
    {
        if (Current.View.TryGetNavigationRequest(out var navigationRequest) && navigationRequest is not null)
        {
            var nextPanel = _listPanelContextFactory.CreateFromNavigation(navigationRequest);
            _contextStack.Push(nextPanel);
            RefreshMainLayout();
        }
    }

    public void HandleResize()
    {
        Current.View.HandleResize();
    }

    public void Dispose()
    {
        _contextStack.Clear();
    }
}