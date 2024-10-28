using Funcy.Infrastructure.Azure;

namespace Funcy.Console;

using Spectre.Console;

public class MainMenuService
{
    private readonly AzureFunctionService _functionService;
    
    public MainMenuService(AzureFunctionService functionService)
    {
        _functionService = functionService;
    }

    public async void ShowMainMenuAsync()
    {
        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Choose an option:")
                .AddChoices(MainMenuConstants.ListFunction, MainMenuConstants.StartFunction,
                    MainMenuConstants.StopFunction, MainMenuConstants.Exit));

        switch (choice)
        {
            case MainMenuConstants.ListFunction:
                await ListFunctions();
                break;
            case MainMenuConstants.StartFunction:
                StartFunction();
                break;
            case MainMenuConstants.StopFunction:
                StopFunction();
                break;
            case MainMenuConstants.Exit:
                Environment.Exit(0);
                break;
        }
    }

    private async Task ListFunctions()
    {
        // Kalla FunctionAppStatusRenderer för att visa en tabell
        AnsiConsole.Markup("[green]Här listas alla funktioner[/]");
        await _functionService.ListFunctionAppsAsync("ee691e14-38ba-4613-91bc-2287244a60e7");
    }

    private void StartFunction()
    {
        var functionName = AnsiConsole.Ask<string>("Ange namnet på funktionen att starta:");
        //_functionService.StartFunctionAsync(functionName).Wait();
        AnsiConsole.Markup($"[green]Startade funktionen {functionName}[/]");
    }

    private void StopFunction()
    {
        var functionName = AnsiConsole.Ask<string>("Ange namnet på funktionen att stoppa:");
        //_functionService.StopFunctionAsync(functionName).Wait();
        AnsiConsole.Markup($"[red]Stoppade funktionen {functionName}[/]");
    }
}