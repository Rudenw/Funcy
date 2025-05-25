using Funcy.Console.Dispatching;
using Funcy.Console.Handlers;
using Funcy.Console.Ui;
using Funcy.Console.Ui.Panels;
using Funcy.Infrastructure.Azure;
using Spectre.Console.Rendering;

namespace Funcy.Console;

using Spectre.Console;

public class MainMenuService(
    IAzureSubscriptionService subscriptionService,
    InputHandler inputHandler,
    ResizeHandler resizeHandler,
    FunctionAppUpdateHandler functionAppUpdateHandler,
    FunctionActionHandler actionHandler)
{
    private MainContainer _mainContainer = null!;

    public async Task StartAsync()
    {
        var subscriptionName = await subscriptionService.GetCurrentSubscriptionName();
        _mainContainer = new MainContainer(subscriptionName, functionAppUpdateHandler.ConsumeChanges());

        var cts = new CancellationTokenSource();
        
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
            functionAppUpdateHandler.WaitForTriggerAsync(),
            actionHandler.WaitForTriggerAsync());
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
                            actionHandler.Dispatch(inputResult);
                        }
                    }

                    if (actionHandler.IsTriggered)
                    {
                        _mainContainer.UpdateFunctionsInDispatch(actionHandler.UncompletedTasks);
                        actionHandler.UncompletedTasks.Clear();
                        
                        var completedTasks = actionHandler.GetAndClearFunctionApps();
                        _mainContainer.UpdatePartialData(completedTasks);
                        
                        actionHandler.ResetTrigger();
                    }
                    
                    if (functionAppUpdateHandler.IsTriggered)
                    {
                        var changes = functionAppUpdateHandler.ConsumeChanges();
                        _mainContainer.UpdatePartialData(changes);

                        var removed = functionAppUpdateHandler.ConsumeRemovedFunctionApps();
                        _mainContainer.RemoveFunctionApps(removed);
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