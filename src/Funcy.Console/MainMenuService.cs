using Funcy.Console.Ui;
using Funcy.Console.Ui.Panels;
using Funcy.Console.Ui.Triggers;
using Funcy.Infrastructure.Azure;
using Funcy.Infrastructure.Model;
using Spectre.Console.Rendering;

namespace Funcy.Console;

using Spectre.Console;

public class MainMenuService(
    AzureFunctionService functionService,
    InputHandler inputHandler,
    TableRowUpdater tableRowUpdater,
    ResizeHandler resizeHandler)
{
    private List<FunctionAppDetails> _functionApps = [];
    private TopPanel? _topPanel;
    private FunctionAppPanel? _functionListPanel;
    
    private readonly List<IPanelController> _panelControllers = [];

    public async Task StartAsync()
    {
        await InitFunctionAppPanel();
        _topPanel = new TopPanel();

        var cts = new CancellationTokenSource();
        
        resizeHandler.StartPolling();
        var inputTask = inputHandler.StartListeningAsync(cts.Token);
        
        await HandleInputAndRenderAsync(cts.Token);
        
        await cts.CancelAsync();
        await inputTask;
        resizeHandler.StopPolling();
    }

    private async Task InitFunctionAppPanel()
    {
        _functionApps = await functionService.InitialLoadFunctionAppsAsync();
        _functionListPanel = new FunctionAppPanel(_functionApps);
        _panelControllers.Add(_functionListPanel);
    }
    
    private async Task WaitForAnyTriggerAsync()
    {
        await Task.WhenAny(
            resizeHandler.WaitForTriggerAsync(),
            inputHandler.WaitForTriggerAsync());
    }

    private async Task HandleInputAndRenderAsync(CancellationToken token)
    {
        _functionListPanel.CreateFunctionAppPanel();

        while (true)
        {
            AnsiConsole.Clear();
            await AnsiConsole.Live(BuildMainLayout()).StartAsync(async ctx =>
            {
                while (!token.IsCancellationRequested)
                {
                    ctx.Refresh();
                    await WaitForAnyTriggerAsync();

                    if (inputHandler.IsTriggered)
                    {
                        await _functionListPanel.HandleInputAsync(inputHandler.TriggeredKey);
                        _functionListPanel.UpdateVisibleTableRows();    
                        inputHandler.ResetTrigger();
                    }
                
                    _functionListPanel.UpdateSelectedTableRow();

                    if (resizeHandler.IsTriggered)
                    {
                        _functionListPanel.OnResize();
                        ctx.UpdateTarget(_functionListPanel.Panel);
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
        // Returnerar ett grid eller en Rows-instans med båda panelerna
        return new Rows(
            _topPanel.Panel,
            _functionListPanel.CreateFunctionAppPanel());
    }
}