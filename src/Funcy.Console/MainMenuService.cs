using Funcy.Console.Ui;
using Funcy.Console.Ui.Panels;
using Funcy.Console.Ui.Triggers;
using Funcy.Infrastructure.Azure;
using Funcy.Infrastructure.Model;
using Spectre.Console.Rendering;

namespace Funcy.Console;

using Spectre.Console;

public class MainMenuService(
    IAzureSubscriptionService subscriptionService,
    InputHandler inputHandler,
    ResizeHandler resizeHandler,
    FunctionAppUpdateHandler functionAppUpdateHandler)
{
    private TopPanel? _topPanel;
    private FunctionAppPanel? _functionListPanel;
    
    private readonly List<IPanelController> _panelControllers = [];

    public async Task StartAsync()
    {
        InitFunctionAppPanel();
        await InitTopPanel();

        var cts = new CancellationTokenSource();
        
        resizeHandler.StartPolling();
        var functionTask = functionAppUpdateHandler.StartListeningAsync(cts.Token);
        var inputTask = inputHandler.StartListeningAsync(cts.Token);
        
        await HandleInputAndRenderAsync(cts.Token);
        
        await cts.CancelAsync();
        await inputTask;
        await functionTask;
        resizeHandler.StopPolling();
    }

    private void InitFunctionAppPanel()
    {
        _functionListPanel = new FunctionAppPanel(functionAppUpdateHandler.FunctionApps);
        _panelControllers.Add(_functionListPanel);
    }
    
    private async Task InitTopPanel()
    {
        var subscriptionName = await subscriptionService.GetCurrentSubscriptionName();
            
        _topPanel = new TopPanel(subscriptionName);
        _panelControllers.Add(_functionListPanel);
    }
    
    private async Task WaitForAnyTriggerAsync()
    {
        await Task.WhenAny(
            resizeHandler.WaitForTriggerAsync(),
            inputHandler.WaitForTriggerAsync(),
            functionAppUpdateHandler.WaitForTriggerAsync());
    }

    private async Task HandleInputAndRenderAsync(CancellationToken token)
    {
        var mainLayout = BuildMainLayout();

        while (true)
        {
            AnsiConsole.Clear();
            await AnsiConsole.Live(mainLayout).StartAsync(async ctx =>
            {
                while (!token.IsCancellationRequested)
                {
                    ctx.Refresh();
                    await WaitForAnyTriggerAsync();

                    if (inputHandler.IsTriggered)
                    {
                        await _functionListPanel.HandleInputAsync(inputHandler.TriggeredKey);
                        inputHandler.ResetTrigger();
                    }
                    
                    if (functionAppUpdateHandler.IsTriggered)
                    {
                        _functionListPanel.OnFunctionAppsUpdated(functionAppUpdateHandler.FunctionApps);
                        functionAppUpdateHandler.ResetTrigger();
                    }
                
                    _functionListPanel.UpdateSelectedTableRow();

                    if (resizeHandler.IsTriggered || functionAppUpdateHandler.IsTriggered)
                    {
                        _functionListPanel.OnResize();
                        //ctx.UpdateTarget(_functionListPanel.Panel);
                        resizeHandler.ResetTrigger();
                        break;
                    }
                }

                return Task.CompletedTask;
            });
        }
    }
    
    private IRenderable BuildMainLayout()
    {
        return new Rows(
            _topPanel.Panel,
            _functionListPanel.CreateFunctionAppPanel());
    }
}