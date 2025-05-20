using System.Text;
using Spectre.Console;
using Spectre.Console.Rendering;
using Funcy.Console.Ui;

namespace Funcy.Console.Ui.Panels;

public class TopPanel : IPanelController
{
    private readonly string _subscriptionName;
    private bool _searchMode;
    private Table _table;
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
        
        _table.AddRow(UiStyles.CreateLabel("Subscription:"),
            new Markup($"{_subscriptionName}"), UiStyles.CreateShortcut("F", "Filter"));
        _table.AddRow(UiStyles.CreateLabel("Filter:"), new Markup(""), UiStyles.CreateShortcut("S", "Start"));
        _table.AddRow(new Markup(""), new Markup(""), UiStyles.CreateShortcut("T", "Stop"));
        _table.AddRow(new Markup(""), new Markup(""), UiStyles.CreateShortcut("W", "Swap"));
    }

    public void HandleInput(ConsoleKeyInfo keyInfo)
    {
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