using Funcy.Infrastructure.Shell;
using Spectre.Console;

namespace Funcy.Console.Ui.Splash;

public class SplashScreenContent
{
    private readonly Table _contentTable = new();

    public Panel Panel { get; }

    public SplashScreenContent()
    {
        _contentTable.Border(TableBorder.None);
        _contentTable.ShowHeaders = false;
        _contentTable.AddColumn("", column => column.Width(113));

        var figlet = new FigletText("Funcy").Color(Color.Orange1);
        _contentTable.AddRow(figlet);
        _contentTable.AddRow(new Markup(""));
        _contentTable.AddRow(new Markup("[orange1]●[/] Initializing..."));

        Panel = new Panel(_contentTable)
        {
            Width = 119,
            Border = BoxBorder.Rounded
        };
        Panel.BorderColor(Color.Orange1);
    }

    public void UpdateInitializingRow(string frame)
    {
        _contentTable.Rows.Update(2, 0, new Markup($"[orange1]{frame}[/] Initializing..."));
    }

    public void ShowValidationErrors(ToolValidationResult? result)
    {
        _contentTable.Rows.RemoveAt(2);
        _contentTable.AddRow(new Markup("[bold red]Missing required tools:[/]"));
        _contentTable.AddRow(new Markup(""));

        if (result?.MissingTools is not null)
        {
            foreach (var tool in result.MissingTools)
            {
                _contentTable.AddRow(new Markup($"  [red]✗[/] {tool}"));
            }
        }

        _contentTable.AddRow(new Markup(""));
        _contentTable.AddRow(new Markup("[bold yellow]Installation instructions:[/]"));
        _contentTable.AddRow(new Markup(""));

        if (result?.InstallInstructions is not null)
        {
            foreach (var instruction in result.InstallInstructions)
            {
                _contentTable.AddRow(new Markup($"  [gray]→[/] {instruction}"));
            }
        }

        _contentTable.AddRow(new Markup(""));
        _contentTable.AddRow(new Markup("[bold red]Please install the missing tools and restart the application.[/]"));
        _contentTable.AddRow(new Markup("[gray]Press any key to exit...[/]"));
        Panel.BorderColor(Color.Red);
    }

    public void ShowAzureLoginPrompt()
    {
        _contentTable.Rows.RemoveAt(2);
        _contentTable.AddRow(new Markup("[bold yellow]Azure CLI is installed but not logged in.[/]"));
        _contentTable.AddRow(new Markup(""));
        _contentTable.AddRow(new Markup("[gray]Press [white]Y[/] to run [white]az login[/] now, or any other key to exit.[/]"));
        Panel.BorderColor(Color.Yellow);
    }

    public void ShowStartupError(Exception exception)
    {
        _contentTable.Rows.RemoveAt(2);
        _contentTable.AddRow(new Markup("[bold red]Could not initialize application:[/]"));
        _contentTable.AddRow(new Markup(""));
        _contentTable.AddRow(new Markup("[yellow]Unexpected startup error.[/]"));
        _contentTable.AddRow(new Markup($"[gray]{Markup.Escape(exception.Message)}[/]"));
        _contentTable.AddRow(new Markup(""));
        _contentTable.AddRow(new Markup("[gray]Press any key to exit...[/]"));
        Panel.BorderColor(Color.Red);
    }

    public void ShowSuccess()
    {
        _contentTable.Rows.Update(2, 0, new Markup("[green]✓[/] All required tools are installed"));
        _contentTable.AddRow(new Markup(""));
        _contentTable.AddRow(new Markup("[gray]Press any key to continue...[/]"));
    }
}
