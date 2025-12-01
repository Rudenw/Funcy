using Funcy.Console.Handlers;
using Funcy.Console.Handlers.Concurrency;
using Funcy.Console.Ui;
using Funcy.Infrastructure.Azure;

namespace Funcy.Console;

using Spectre.Console;

public class AppOrchestrator(
    IAzureSubscriptionService subscriptionService,
    InputHandler inputHandler,
    ResizeHandler resizeHandler,
    FunctionAppUpdateHandler functionAppUpdateHandler,
    FunctionActionHandler actionHandler,
    FunctionStateCoordinator functionStateCoordinator)
{
    private MainContainer _mainContainer = null!;

    public async Task StartAsync()
    {
        var cts = new CancellationTokenSource();
        
        var subscriptionName = await subscriptionService.GetCurrentSubscriptionName();
        await functionAppUpdateHandler.InitializeAsync(cts.Token);
        
        _mainContainer = new MainContainer(subscriptionName, functionStateCoordinator.GetInitialLoad(), functionStateCoordinator, actionHandler);

        var resizeTask = resizeHandler.StartPolling(cts.Token);
        var functionTask = functionAppUpdateHandler.StartListeningAsync(cts.Token);
        var inputTask = inputHandler.StartListeningAsync(cts.Token);
        var actionTask = actionHandler.StartListeningAsync(cts.Token);
        
        await HandleInputAndRenderAsync(cts.Token);
        
        await cts.CancelAsync();
        await inputTask;
        await functionTask;
        await resizeTask;
        await actionTask;
    }
    
    private async Task WaitForAnyTriggerAsync()
    {
        await Task.WhenAny(
            resizeHandler.WaitForTriggerAsync(),
            inputHandler.WaitForTriggerAsync(),
            _mainContainer.WaitForTriggerAsync());
    }

    private async Task HandleInputAndRenderAsync(CancellationToken token)
    {
        var mainLayout = _mainContainer.MainLayout;

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
                        _mainContainer.HandleInput(inputHandler.TriggeredKeyInfo);
                        inputHandler.ResetTrigger();
                    }
                    
                    if (resizeHandler.IsTriggered)
                    {
                        _mainContainer.HandleResize();
                        resizeHandler.ResetTrigger();
                        break;
                    }

                    if (_mainContainer.WaitForTriggerAsync().IsCompleted)
                    {
                        _mainContainer.HandleUpdate();
                        _mainContainer.ResetTrigger();
                    }
                }

                return Task.CompletedTask;
            });
        }
    }
}