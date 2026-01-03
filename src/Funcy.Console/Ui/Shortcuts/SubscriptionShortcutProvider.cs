using Funcy.Core.Model;

namespace Funcy.Console.Ui.Shortcuts;

public class SubscriptionShortcutProvider : IShortcutProvider<SubscriptionDetails>
{
    public Dictionary<TableIndex, ShortcutMap> Describe(SubscriptionDetails? slotDetails)
    {
        var shortcutList = new Dictionary<TableIndex, ShortcutMap>
        {
            {new TableIndex(0, 2), new ShortcutMap(ListPanelShortcuts.Filter, true)},
        };
        return shortcutList;
    }

    public bool IsActionValid(SubscriptionDetails getSelectedItem, FunctionAction action)
    {
        return false;
    }
}