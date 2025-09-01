using Spectre.Console;
using Funcy.Core.Model;


namespace Funcy.Console.Ui.Panels;

public class TopPanel
{
    private readonly string _subscriptionName;
    private readonly Table _table;
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

        _table.AddRow(UiStyles.CreateLabelMarkup("Subscription:"), new Markup($"{_subscriptionName}"),
            UiStyles.CreateShortcutMarkup(ListPanelShortcuts.Filter.DisplayChar, ListPanelShortcuts.Filter.Label),
            UiStyles.CreateShortcutMarkup(ListPanelShortcuts.Swap.DisplayChar, ListPanelShortcuts.Swap.Label));
        
        _table.AddRow(UiStyles.CreateLabelMarkup("Filter:"), new Markup(""),
            UiStyles.CreateShortcutMarkup(ListPanelShortcuts.Start.DisplayChar, ListPanelShortcuts.Start.Label),
            UiStyles.CreateShortcutMarkup(ListPanelShortcuts.Stop.DisplayChar, ListPanelShortcuts.Stop.Label));
    }
    
    public void SetSearchText(Markup searchMarkup)
    {
        UpdateSearchCell(searchMarkup);
    }
    
    private void UpdateSearchCell(Markup searchText)
    {
        _table.Rows.Update(1, 1, searchText);
    }
}