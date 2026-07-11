using Funcy.Core.Model;

namespace Funcy.Console.Ui.Shortcuts;

public class LogEntryShortcutProvider : IShortcutProvider<LogEntryDetails>
{
    public Dictionary<TableIndex, ShortcutMap> Describe(LogEntryDetails? item)
    {
        return new Dictionary<TableIndex, ShortcutMap>
        {
            { new TableIndex(0, 2), new ShortcutMap(ListPanelShortcuts.Filter, true) },
            { new TableIndex(0, 3), new ShortcutMap(ListPanelShortcuts.TypeFilter, true) },
            { new TableIndex(0, 4), new ShortcutMap(ListPanelShortcuts.Refresh, true) },
            // Copy the selected entry's full message; disabled when nothing is selected.
            { new TableIndex(0, 5), new ShortcutMap(ListPanelShortcuts.CopyMessage, item is not null) },
            // The lookback range: shorter (,) and longer (.).
            { new TableIndex(1, 2), new ShortcutMap(ListPanelShortcuts.RangeShorter, true) },
            { new TableIndex(1, 3), new ShortcutMap(ListPanelShortcuts.RangeLonger, true) },
        };
    }

    // Refresh/type-filter are handled directly by the controller, not the action pipeline.
    public bool IsActionValid(LogEntryDetails? getSelectedItem, FunctionAction action) => false;
}
