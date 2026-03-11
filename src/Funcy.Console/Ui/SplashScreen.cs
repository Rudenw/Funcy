using Funcy.Console.Handlers;
using Funcy.Console.Ui.Splash;
using Funcy.Infrastructure.Shell;

namespace Funcy.Console.Ui;

using Spectre.Console;

public class SplashScreen
{
    private readonly ToolValidationService _toolValidationService;
    private readonly AnimationHandler _animationHandler;
    private readonly SplashScreenContent _content = new();
    private const string SplashAnimationKey = "SplashScreen";

    public SplashScreen(ToolValidationService toolValidationService, AnimationHandler animationHandler)
    {
        _toolValidationService = toolValidationService;
        _animationHandler = animationHandler;
    }

    public async Task<bool> ShowAsync(Task[] backgroundTasks, Func<Task>? continuationTask = null)
    {
        AnsiConsole.Clear();
        AnsiConsole.Cursor.Hide();

        ToolValidationResult? validationResult = null;
        Exception? startupException = null;

        _animationHandler.AddAppDetails(SplashAnimationKey);

        await AnsiConsole.Live(_content.Panel)
            .StartAsync(async ctx =>
            {
                ctx.Refresh();

                var validationTask = _toolValidationService.ValidateRequiredToolsAsync();
                var allTasks = Task.WhenAll(backgroundTasks.Append(validationTask));

                while (!allTasks.IsCompleted)
                {
                    await Task.WhenAny(allTasks, _animationHandler.WaitForTriggerAsync());

                    if (_animationHandler.IsTriggered)
                    {
                        var frame = _animationHandler.GetAnimation(SplashAnimationKey)?.AnimationFrame ?? "●";
                        _content.UpdateInitializingRow(frame);
                        _animationHandler.ResetTrigger();
                        ctx.Refresh();
                    }
                }

                try
                {
                    await allTasks;
                    validationResult = await validationTask;

                    if (validationResult is not null && validationResult.IsValid && continuationTask is not null)
                    {
                        await continuationTask();
                    }
                }
                catch (Exception ex)
                {
                    startupException = ex;
                }

                _animationHandler.RemoveAppDetails(SplashAnimationKey);

                if (startupException is not null && SplashStartupErrorClassifier.IsAzureNotLoggedInError(startupException))
                {
                    _content.ShowAzureLoginPrompt();
                }
                else if (startupException is not null)
                {
                    _content.ShowStartupError(startupException);
                }
                else if (validationResult is null || !validationResult.IsValid)
                {
                    _content.ShowValidationErrors(validationResult);
                }
                else
                {
                    _content.ShowSuccess();
                }

                ctx.Refresh();
            });

        System.Console.CursorVisible = false;

        if (startupException is not null)
        {
            if (!SplashStartupErrorClassifier.IsAzureNotLoggedInError(startupException))
            {
                System.Console.ReadKey(true);
                return false;
            }

            var wantsLogin = AzureLoginWorkflow.PromptForAzureLogin();
            if (!wantsLogin)
            {
                return false;
            }

            var loginSucceeded = await AzureLoginWorkflow.TryAzureLoginAsync();
            if (!loginSucceeded)
            {
                return false;
            }

            if (continuationTask is null)
            {
                return true;
            }

            try
            {
                await continuationTask();
                return true;
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Login succeeded but startup still failed:[/] [gray]{Markup.Escape(ex.Message)}[/]");
                AnsiConsole.MarkupLine("[gray]Press any key to exit...[/]");
                System.Console.ReadKey(true);
                return false;
            }
        }

        if (validationResult is null || !validationResult.IsValid)
        {
            System.Console.ReadKey(true);
            return false;
        }

        System.Console.ReadKey(true);
        return true;
    }
}
