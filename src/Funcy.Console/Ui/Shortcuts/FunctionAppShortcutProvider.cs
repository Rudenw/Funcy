using Funcy.Core.Model;

namespace Funcy.Console.Ui.Shortcuts;

public class FunctionAppShortcutProvider : IShortcutProvider<FunctionAppDetails>
{
    public List<ShortcutMap> Describe(FunctionAppDetails? app)
    {
        var shortcutList = new List<ShortcutMap>
        {
            new(ListPanelShortcuts.Filter, 0, 2, true),
            new(ListPanelShortcuts.Start, 1, 2, CanStart(app)),
            new(ListPanelShortcuts.Stop, 1, 3, CanStop(app)),
            new(ListPanelShortcuts.Swap, 0, 3, CanSwap(app))
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