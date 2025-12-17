using Funcy.Core.Model;
using Spectre.Console;

namespace Funcy.Console.Ui;

public static class UiStyles
{
    private static readonly bool Unicode = AnsiConsole.Profile.Capabilities.Unicode;
    
    public const string Label = "bold yellow";
    public const string Shortcut = "bold purple_2";
    public const string Danger = "bold red";
    public const string Hint = "gray";
    public const string Sort = "yellow";
    
    private static readonly string ArrowUp = Unicode ? "↑" : "^";
    private static readonly string ArrowDown = Unicode ? "↓" : "v";

    public static Markup CreateLabelMarkup(string text) => new($"[{Label}]{text}[/]");

    public static Markup CreateShortcutMarkup(string shortcut, string description, bool isEnabled = true)
        => new($"[{(isEnabled ? Shortcut : Hint)}]<{shortcut}>[/] [{Hint}]{description}[/]");

    public static string CreateDangerText(string text) => $"[{Danger}]{text}[/]";

    public static string CreateHeaderText(string text, int? index, bool descending, bool isActiveColumn = false)
    {
        var arrow = isActiveColumn ? (descending ? ArrowDown : ArrowUp) : "";
        var sorting = index is not null ? $"[{Sort}]({index}) {arrow}[/]" : "";
        return $"[bold]{text}[/]{sorting}";
    }

    public static Markup CreateSelectedCell(string text)
        => new("[black on yellow]" + text + "[/]");

    public static Markup CreateStateCell(FunctionState state)
        => new($"[bold {UiHelper.GetStateColor(state)}]{state.ToDisplayLabel()}[/]");

    public static Markup CreateStatusCell(FunctionStatus status)
    {
        return new Markup($"[bold {UiHelper.GetStatusColor(status)}]{status.ToDisplayLabel()}[/]");
    }
}
