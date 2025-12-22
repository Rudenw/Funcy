using Funcy.Console.Handlers;
using Funcy.Console.Handlers.Concurrency;
using Funcy.Console.Ui;
using Funcy.Console.Ui.Factory;
using Funcy.Console.Ui.State;
using Funcy.Infrastructure.Azure;

namespace Funcy.Console;

using Spectre.Console;

public class AppOrchestrator(
    IAzureResourceService resourceService,
    InputHandler inputHandler,
    ResizeHandler resizeHandler,
    AnimationHandler animationHandler,
    FunctionAppUpdateHandler functionAppUpdateHandler,
    FunctionActionHandler actionHandler,
    FunctionStateCoordinator functionStateCoordinator,
    ListPanelContextFactory listPanelContextFactory,
    UiStateMarkupProvider uiStateMarkupProvider,
    AppContext appContext)
{
    private MainContainer _mainContainer = null!;

    public async Task StartAsync()
    {
        var cts = new CancellationTokenSource();

        await appContext.InitializeAppContext();
        var subscriptionName = await resourceService.GetCurrentSubscriptionName();
        await functionAppUpdateHandler.InitializeAsync(cts.Token);

        _mainContainer = new MainContainer(subscriptionName, functionStateCoordinator.GetCachedFunctionAppDetails(),
            listPanelContextFactory, actionHandler, functionAppUpdateHandler, uiStateMarkupProvider);
        _ = functionAppUpdateHandler.StartListeningAsync(cts.Token);
        
        var resizeTask = resizeHandler.StartPolling(cts.Token);
        var inputTask = inputHandler.StartListeningAsync(cts.Token);
        var animation = animationHandler.StartAsync(cts.Token);
        
        await HandleInputAndRenderAsync(cts.Token);
        
        await animation;
        await cts.CancelAsync();
        await inputTask;
        await resizeTask;
    }
    
    private async Task WaitForAnyTriggerAsync()
    {
        await Task.WhenAny(
            resizeHandler.WaitForTriggerAsync(),
            inputHandler.WaitForTriggerAsync(),
            _mainContainer.WaitForTriggerAsync(),
        animationHandler.WaitForTriggerAsync());
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
                    
                    if (animationHandler.IsTriggered)
                    {
                        _mainContainer.HandleAnimation();
                        animationHandler.ResetTrigger();
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