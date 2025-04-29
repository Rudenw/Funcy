using Funcy.Console.Ui;
using Funcy.Infrastructure.Azure;
using Funcy.Infrastructure.Model;

namespace Funcy.Console;

using Spectre.Console;

public class MainMenuService(
    AzureFunctionService functionService,
    InputHandler inputHandler,
    TableRowUpdater tableRowUpdater,
    ResizeHandler resizeHandler)
{
    private List<FunctionAppDetails> _functionApps = [];
    private FunctionAppPanel _functionAppPanel;
    public TaskCompletionSource ResizeUpdateTrigger { get; private set; } = new();

    public async Task StartMonitoringAsync()
    {
        _functionApps = await functionService.InitialLoadFunctionAppsAsync();
        _functionAppPanel = new FunctionAppPanel(_functionApps, tableRowUpdater);
        var cts = new CancellationTokenSource();
        
        resizeHandler.OnResize += (width, height) =>
        {
            lock (ResizeUpdateTrigger)
            {
                ResizeUpdateTrigger.TrySetResult();
            }
        };

        //var updateTask = UpdateDataAsync(cts.Token);
        resizeHandler.StartPolling();

        var inputTask = inputHandler.HandleInputAsync(_functionAppPanel.MaxVisibleRows, _functionApps.Count, cts.Token);
        
        await HandleInputAndRenderAsync(cts.Token);
        
        await cts.CancelAsync();
        //await updateTask;
        await inputTask;
    }

    private async Task HandleInputAndRenderAsync(CancellationToken token)
    {
        AnsiConsole.Clear();
        _functionAppPanel.CreateFunctionAppPanel();
        tableRowUpdater.UpdateSelectedTableRow(_functionAppPanel.Table.Rows, 0, 0); //TODO: inte här

        while (true)
        {
            await AnsiConsole.Live(_functionAppPanel.Panel).StartAsync(ctx =>
            {
                while (!token.IsCancellationRequested)
                {
                    ctx.Refresh();
                    Task.WaitAny(ResizeUpdateTrigger.Task, inputHandler.UpdateTrigger.Task);

                    if (inputHandler.IsVisibleStartIndexChanged)
                    {
                        tableRowUpdater.UpdateVisibleTableRows(_functionAppPanel.Table, inputHandler.VisibleStartIndex, _functionAppPanel.MaxVisibleRows);    
                    }
                
                    tableRowUpdater.UpdateSelectedTableRow(_functionAppPanel.Table.Rows, inputHandler.OldSelectedIndex,
                        inputHandler.SelectedIndex);

                    if (inputHandler.UpdateTrigger.Task.IsCompleted)
                    {
                        inputHandler.ResetUpdateTrigger();
                    }

                    if (ResizeUpdateTrigger.Task.IsCompleted)
                    {
                        AnsiConsole.Clear();
                        _functionAppPanel.UpdateMaxVisibleRows();
                        _functionAppPanel.CreateFunctionAppPanel();
                        tableRowUpdater.UpdateSelectedTableRow(_functionAppPanel.Table.Rows, inputHandler.OldSelectedIndex, inputHandler.SelectedIndex); //TODO: inte här
                        ctx.UpdateTarget(_functionAppPanel.Panel);
                        ResizeUpdateTrigger = new TaskCompletionSource();
                        break;
                    }
                }

                return Task.CompletedTask;
            });
        }
    }
}