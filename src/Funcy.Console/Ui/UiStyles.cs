using Spectre.Console;

namespace Funcy.Console.Ui;

public static class UiStyles
{
    public const string Label = "bold yellow";
    public const string Shortcut = "bold purple_2";
    public const string Danger = "bold red";
    public const string Hint = "gray";

    public static Markup CreateLabelMarkup(string text) => new($"[{Label}]{text}[/]");

    public static Markup CreateShortcutMarkup(string shortcut, string description, bool isEnabled = true)
        => new($"[{(isEnabled ? Shortcut : Hint)}]<{shortcut}>[/] [{Hint}]{description}[/]");

    public static string CreateDangerText(string text) => $"[{Danger}]{text}[/]";

    public static string CreateHeaderText(string text) => $"[bold]{text}[/]";

    public static Markup CreateSelectedCell(string text)
        => new("[black on yellow]" + text + "[/]");

    public static Markup CreateStatusCell(string state)
        => new($"[bold {UiHelper.GetStatusColor(state)}]{state}[/]");
}
