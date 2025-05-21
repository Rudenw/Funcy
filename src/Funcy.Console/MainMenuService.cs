using Funcy.Console.Dispatching;
using Funcy.Console.Input;
using Funcy.Console.Ui;
using Funcy.Console.Ui.Panels;
using Funcy.Console.Ui.Triggers;
using Funcy.Infrastructure.Azure;
using Spectre.Console.Rendering;

namespace Funcy.Console;

using Spectre.Console;

public class MainMenuService(
    IAzureSubscriptionService subscriptionService,
    InputHandler inputHandler,
    ResizeHandler resizeHandler,
    FunctionAppUpdateHandler functionAppUpdateHandler,
    FunctionActionDispatcher actionDispatcher)
{
    private MainContainer _mainContainer = null!;

    public async Task StartAsync()
    {
        var subscriptionName = await subscriptionService.GetCurrentSubscriptionName();
        _mainContainer = new MainContainer(subscriptionName, functionAppUpdateHandler.FunctionApps);

        var cts = new CancellationTokenSource();
        
        var resizeTask = resizeHandler.StartPolling(cts.Token);
        var functionTask = functionAppUpdateHandler.StartListeningAsync(cts.Token);
        var inputTask = inputHandler.StartListeningAsync(cts.Token);
        
        await HandleInputAndRenderAsync(cts.Token);
        
        await cts.CancelAsync();
        await inputTask;
        //await functionTask;
        await resizeTask;
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
        var mainLayout = _mainContainer.BuildMainLayout();

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
                        var inputResult = _mainContainer.HandleInput(inputHandler.TriggeredKeyInfo);
                        inputHandler.ResetTrigger();

                        if (inputResult is not null)
                        {
                            actionDispatcher.Dispatch(inputResult);
                        }
                    }
                    
                    if (functionAppUpdateHandler.IsTriggered)
                    {
                        _mainContainer.UpdateData(functionAppUpdateHandler.FunctionApps);
                        functionAppUpdateHandler.ResetTrigger();
                    }
                    
                    if (resizeHandler.IsTriggered || functionAppUpdateHandler.IsTriggered)
                    {
                        _mainContainer.HandleResize();
                        resizeHandler.ResetTrigger();
                        break;
                    }
                }

                return Task.CompletedTask;
            });
        }
    }
}