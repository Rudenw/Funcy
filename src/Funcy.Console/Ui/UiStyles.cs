using Spectre.Console;

namespace Funcy.Console.Ui;

public static class UiStyles
{
    public const string Label = "bold yellow";
    public const string Shortcut = "bold purple_2";
    public const string Danger = "bold red";
    public const string Hint = "gray";

    public static Markup CreateLabel(string text) => new($"[{Label}]{text}[/]");

    public static Markup CreateShortcut(string shortcut, string description)
        => new($"[{Shortcut}]<{shortcut}>[/] [{Hint}]{description}[/]");

    public static Markup CreateDanger(string text) => new($"[{Danger}]{text}[/]");

    public static Markup CreateHint(string text) => new($"[{Hint}]{text}[/]");

    public static Markup CreateHeader(string text) => new($"[bold]{text}[/]");

    public static Markup CreateSelectedCell(string text)
        => new("[black on yellow]" + text + "[/]");

    public static Markup CreateStatusCell(string state)
        => new($"[bold {UiHelper.GetStatusColor(state)}]{state}[/]");
}
