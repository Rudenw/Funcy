using System.Text;
using Spectre.Console;
using Spectre.Console.Rendering;

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
        
        _table.AddRow(new Markup($"[bold yellow]Subscription:[/]"),
            new Markup($"{_subscriptionName}"), new Markup("[bold purple_2]<F>[/] [gray]Filter[/]"));
        _table.AddRow(new Markup($"[bold yellow]Filter: [/]"), new Markup(""), new Markup("[bold purple_2]<S>[/] [gray]Start[/]"));
        _table.AddRow(new Markup(""), new Markup(""), new Markup("[bold purple_2]<T>[/] [gray]Stop[/]"));
        _table.AddRow(new Markup(""), new Markup(""), new Markup("[bold purple_2]<W>[/] [gray]Swap[/]"));
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