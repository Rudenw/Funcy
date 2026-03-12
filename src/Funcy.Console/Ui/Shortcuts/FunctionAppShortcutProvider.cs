using Funcy.Console.Ui.State;
using Funcy.Core.Model;

namespace Funcy.Console.Ui.Shortcuts;

public class FunctionAppShortcutProvider(IUiStatusState uiStatusState) : IShortcutProvider<FunctionAppDetails>
{
    public Dictionary<TableIndex, ShortcutMap> Describe(FunctionAppDetails? app)
    {
        var shortcutList = new Dictionary<TableIndex, ShortcutMap>
        {
            {new TableIndex(0, 2), new ShortcutMap(ListPanelShortcuts.Filter, true)},
            {new TableIndex(0, 3), new ShortcutMap(ListPanelShortcuts.ChangeSubscription, true)},
            {new TableIndex(0, 4), new ShortcutMap(ListPanelShortcuts.Refresh, CanRefresh(app))},
            {new TableIndex(1, 2), new ShortcutMap(ListPanelShortcuts.Start, CanStart(app))},
            {new TableIndex(1, 3), new ShortcutMap(ListPanelShortcuts.Stop, CanStop(app))},
            {new TableIndex(1, 4), new ShortcutMap(ListPanelShortcuts.RefreshAll, CanRefreshAll())}
        };
        return shortcutList;
    }

    public bool IsActionValid(FunctionAppDetails getSelectedItem, FunctionAction action)
    {
        return action switch
        {
            FunctionAction.Start => CanStart(getSelectedItem),
            FunctionAction.Stop => CanStop(getSelectedItem),
            FunctionAction.Swap => CanSwap(getSelectedItem),
            FunctionAction.Refresh => CanRefresh(getSelectedItem),
            FunctionAction.RefreshAll => CanRefreshAll(),
            FunctionAction.ChangeSubscription => true,
            _ => false
        };
    }

    private bool CanRefreshAll()
    {
        var status = uiStatusState.GetSnapshot();
        return !status.IsInventoryValidating && !status.IsDetailsRefreshing;
    }

    private static bool CanStart(FunctionAppDetails? app) =>
        app is not null && app.State == FunctionState.Stopped && app.Status.Status != StatusType.InProgress;

    private static bool CanStop(FunctionAppDetails? app) =>
        app is not null && app.State == FunctionState.Running && app.Status.Status != StatusType.InProgress;

    private static bool CanSwap(FunctionAppDetails? app) =>
        app is not null && app.Status.Status != StatusType.InProgress && app.Slots.Count >= 0;

    private static bool CanRefresh(FunctionAppDetails? app) =>
        app is not null && app.Status.Status != StatusType.InProgress;
}
