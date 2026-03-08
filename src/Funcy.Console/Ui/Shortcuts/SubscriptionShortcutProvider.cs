using Funcy.Console;
using Funcy.Core.Model;

namespace Funcy.Console.Ui.Shortcuts;

public class SubscriptionShortcutProvider(AppContext appContext) : IShortcutProvider<SubscriptionDetails>
{
    public Dictionary<TableIndex, ShortcutMap> Describe(SubscriptionDetails? slotDetails)
    {
        var hideShortcut = appContext.HideEmptySubscriptions ? ListPanelShortcuts.ShowAll : ListPanelShortcuts.HideEmpty;
        return new Dictionary<TableIndex, ShortcutMap>
        {
            { new TableIndex(0, 2), new ShortcutMap(ListPanelShortcuts.Filter, true) },
            { new TableIndex(0, 3), new ShortcutMap(hideShortcut, true) },
        };
    }

    public bool IsActionValid(SubscriptionDetails getSelectedItem, FunctionAction action)
    {
        return action switch
        {
            FunctionAction.HideSubscription => true,
            _ => false
        };
    }
}
