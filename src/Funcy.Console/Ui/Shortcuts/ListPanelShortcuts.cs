namespace Funcy.Console;

public static class ListPanelShortcuts
{
    public static readonly Shortcut Filter = new(ConsoleKey.F, "F", "Filter");
    public static readonly Shortcut Start = new(ConsoleKey.S, "S", "Start");
    public static readonly Shortcut Stop = new(ConsoleKey.T, "T", "Stop");
    public static readonly Shortcut Swap = new(ConsoleKey.W, "W", "Swap");

    public static readonly IReadOnlyList<Shortcut> All = [Filter, Start, Stop, Swap];
}

public record TableIndex(int Row, int Column);
public record Shortcut(ConsoleKey Key, string DisplayChar, string Label);
public record ShortcutMap(Shortcut Shortcut, bool IsEnabled);