using Funcy.Core.Model;

namespace Funcy.Console.Ui.Shortcuts;

public class FunctionAppShortcutProvider : IShortcutProvider<FunctionAppDetails>
{
    public Dictionary<TableIndex, ShortcutMap> Describe(FunctionAppDetails? app)
    {
        var shortcutList = new Dictionary<TableIndex, ShortcutMap>
        {
            {new TableIndex(0, 2), new ShortcutMap(ListPanelShortcuts.Filter, true)},
            {new TableIndex(1, 2), new ShortcutMap(ListPanelShortcuts.Start, CanStart(app))},
            {new TableIndex(1, 3), new ShortcutMap(ListPanelShortcuts.Stop, CanStop(app))},
            {new TableIndex(0, 3), new ShortcutMap(ListPanelShortcuts.Swap, CanSwap(app))}
        };
        return shortcutList;
    }

    private static bool CanStart(FunctionAppDetails? app) =>
        app is null or { State: FunctionState.Stopped, Status.Status: StatusType.Idle };

    private static bool CanStop(FunctionAppDetails? app) =>
        app is null or { State: FunctionState.Running, Status.Status: StatusType.Idle };

    private static bool CanSwap(FunctionAppDetails? app) =>
        app is null or { Status.Status: StatusType.Idle, Slots.Count: >= 0 };
}