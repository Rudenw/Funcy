using Funcy.Infrastructure.Shell;
using Spectre.Console;

namespace Funcy.Console.Ui.Splash;

public static class AzureLoginWorkflow
{
    public static bool PromptForAzureLogin()
    {
        AnsiConsole.MarkupLine("[yellow]Azure CLI session saknas. Vill du logga in nu?[/] [gray](Y/n)[/]");
        var key = System.Console.ReadKey(true);
        return key.Key is ConsoleKey.Y or ConsoleKey.Enter;
    }

    public static async Task<bool> TryAzureLoginAsync()
    {
        AnsiConsole.Clear();
        AnsiConsole.MarkupLine("[orange1]Startar Azure-login...[/]");
        var exitCode = await ShellCommandRunner.RunInteractiveAsync("az", "login");
        if (exitCode == 0)
        {
            AnsiConsole.MarkupLine("[green]Inloggning klar. Fortsätter uppstart...[/]");
            return true;
        }

        AnsiConsole.MarkupLine("[red]Azure login misslyckades.[/]");
        AnsiConsole.MarkupLine("[gray]Press any key to exit...[/]");
        System.Console.ReadKey(true);
        return false;
    }
}
