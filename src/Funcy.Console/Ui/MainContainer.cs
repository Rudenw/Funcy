using Funcy.Console.Handlers;
using Funcy.Console.Handlers.Concurrency;
using Funcy.Console.Ui.Controllers;
using Funcy.Console.Ui.Factory;
using Funcy.Console.Ui.Input;
using Funcy.Console.Ui.Panels;
using Funcy.Console.Ui.Panels.Interfaces;
using Funcy.Core.Model;
using Spectre.Console;

namespace Funcy.Console.Ui;

public class MainContainer : IDisposable
{
    private readonly FunctionActionHandler _functionActionHandler;
    private readonly TopPanel _topPanel;
    private readonly SearchInputManager _searchInput = new();
    private TaskCompletionSource _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    
    private readonly Stack<IListPanel> _bodyPanelStack = new();
    
    public readonly Layout MainLayout;
    private bool _searchMode;
    private readonly ListPanelFactory _listPanelFactory;
    private readonly IListController _rootController;

    public MainContainer(string subscriptionName,
        List<FunctionAppDetails> functionApps,
        FunctionStateCoordinator functionStateCoordinator,
        FunctionActionHandler functionActionHandler)
    {
        _functionActionHandler = functionActionHandler;
        _topPanel = new TopPanel(subscriptionName);

        _listPanelFactory = new ListPanelFactory(functionStateCoordinator.TryGet);
        var functionAppListPanel = _listPanelFactory.CreateFunctionAppPanel(functionApps);
        _rootController = new FunctionAppListController(
            (IListPanelView<FunctionAppDetails>) functionAppListPanel,
            functionApps,
            functionStateCoordinator,
            invalidate: () => _tcs.TrySetResult()
        );

        _bodyPanelStack.Push(functionAppListPanel);
        
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
                case var key when key == ListPanelShortcuts.Filter.Key:
                    _searchMode = true;
                    _searchInput.InitializeSearchMode();
                    break;
                case var key when key == ListPanelShortcuts.Start.Key:
                    action =  FunctionAction.Start;
                    break;
                case var key when key == ListPanelShortcuts.Stop.Key:
                    action =  FunctionAction.Stop;
                    break;
                case var key when key == ListPanelShortcuts.Swap.Key:
                    action =  FunctionAction.Swap;
                    break;
                case ConsoleKey.Delete:
                    _searchInput.ClearSearchText();
                    break;
                case ConsoleKey.Enter:
                    PushNextPanel();
                    break;
            }
        }
        
        _topPanel.SetSearchText(_searchInput.SearchMarkup);
        _bodyPanelStack.Peek().SetSearchText(_searchInput.SearchText);

        if (action is not null)
        {
            var current = _bodyPanelStack.Peek();
            if (current is IActionHandlingPanel actionPanel &&
                actionPanel.TryBuildAction(action.Value, out var input))
            {
                _ = _functionActionHandler.Dispatch(input);
            }
            else
            {
                if (current.TryGetActionNavigationRequest(out var navigationRequest) && navigationRequest is not null)
                {
                    var nextPanel = _listPanelFactory.Create(navigationRequest);
                    _bodyPanelStack.Push(nextPanel);
                    RefreshMainLayout();
                }
            }
        }

        if (keyInfo.Key is ConsoleKey.Escape or ConsoleKey.Spacebar && _bodyPanelStack.Count > 1)
        {
            _bodyPanelStack.Pop();
            RefreshMainLayout();
        }
        
        _bodyPanelStack.Peek().HandleInput(keyInfo);
    }

    private void PushNextPanel()
    {
        if (_bodyPanelStack.Peek().TryGetNavigationRequest(out var navigationRequest) && navigationRequest is not null)
        {
            var nextPanel = _listPanelFactory.Create(navigationRequest);
            _bodyPanelStack.Push(nextPanel);
            RefreshMainLayout();
        }
    }

    public void HandleResize()
    {
        _bodyPanelStack.Peek().HandleResize();
    }

    public void Dispose()
    {
        _rootController.Dispose();
    }
}