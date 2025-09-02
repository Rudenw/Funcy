using Spectre.Console;
using Funcy.Core.Model;


namespace Funcy.Console.Ui.Panels;

public class TopPanel
{
    private readonly string _subscriptionName;
    private readonly Table _table;
    private readonly Dictionary<TableIndex, ShortcutMap> _renderedShortcuts = new();
    public Panel Panel { get; }

    public TopPanel(string subscriptionName)
    {
        _subscriptionName = subscriptionName;
        
        _table = new Table();
        RenderTableLayout();
            
        Panel = new Panel(_table);
        Panel.Width = 104;
        Panel.Border(BoxBorder.Ascii);
    }

    private void RenderTableLayout()
    {
        _table.Border(TableBorder.None);
        _table.ShowHeaders = false;
        
        _table.AddColumn("", column => column.Width = 15);
        _table.AddColumn("", column =>
        {
            column.Width = 30;
            column.LeftAligned();
        });
        _table.AddColumn("", column =>
        {
            column.Width = 30;
            column.LeftAligned();
        });
        _table.AddColumn("", column =>
        {
            column.Width = 30;
            column.LeftAligned();
        });
            
        _renderedShortcuts.Add(new TableIndex(0, 2), new ShortcutMap(ListPanelShortcuts.Filter, true));
        _renderedShortcuts.Add(new TableIndex(1, 2), new ShortcutMap(ListPanelShortcuts.Start, true));
        _renderedShortcuts.Add(new TableIndex(1, 3), new ShortcutMap(ListPanelShortcuts.Stop, true));
        _renderedShortcuts.Add(new TableIndex(0, 3), new ShortcutMap(ListPanelShortcuts.Swap, true));

        _table.AddRow(UiStyles.CreateLabelMarkup("Subscription:"), new Markup($"{_subscriptionName}"), new Markup(""), new Markup(""));
        _table.AddRow(UiStyles.CreateLabelMarkup("Filter:"), new Markup(""), new Markup(""), new Markup(""));

        UpdateShortcuts();
    }

    private void UpdateShortcuts()
    {
        foreach (var shortcut in _renderedShortcuts)
        {
            _table.Rows.Update(shortcut.Key.Row, shortcut.Key.Column,
                UiStyles.CreateShortcutMarkup(shortcut.Value.Shortcut.DisplayChar, shortcut.Value.Shortcut.Label,
                    shortcut.Value.IsEnabled));
        }
    }
    
    public void SetSearchText(Markup searchMarkup)
    {
        UpdateSearchCell(searchMarkup);
    }
    
    private void UpdateSearchCell(Markup searchText)
    {
        _table.Rows.Update(1, 1, searchText);
    }

    public void UpdateShortcuts(Dictionary<TableIndex, ShortcutMap> next)
    {
        foreach (var shortcut in next)
        {
            if (!_renderedShortcuts.TryGetValue(shortcut.Key, out var oldMap) || oldMap != shortcut.Value)
            {
                var markup = UiStyles.CreateShortcutMarkup(shortcut.Value.Shortcut.DisplayChar,
                    shortcut.Value.Shortcut.Label, shortcut.Value.IsEnabled);
                _table.Rows.Update(shortcut.Key.Row, shortcut.Key.Column, markup);
                
                _renderedShortcuts[shortcut.Key] = shortcut.Value;
            }
            
            _table.Rows.Update(shortcut.Key.Row, shortcut.Key.Column,
                UiStyles.CreateShortcutMarkup(shortcut.Value.Shortcut.DisplayChar, shortcut.Value.Shortcut.Label,
                    shortcut.Value.IsEnabled));
        }
        
        foreach (var removedKey in _renderedShortcuts.Keys.Except(next.Keys))
        {
            _table.Rows.Update(removedKey.Row, removedKey.Column, new Markup(""));
            _renderedShortcuts.Remove(removedKey);
        }
    }
}