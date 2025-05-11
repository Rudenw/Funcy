using Funcy.Console.Models;
using Funcy.Infrastructure.Model;
using Spectre.Console;

namespace Funcy.Console.Ui.Renderers;

public class FunctionAppTableRenderer
{
    public Table Table { get; set; }

    public FunctionAppTableRenderer(int width = 100)
    {
        Table = new Table();
        Table.Border(TableBorder.None);
        Table.Width(width);
        Table.AddColumn(" ");
        Table.AddColumn("[bold]Name[/]");
        Table.AddColumn("[bold]Status[/]");
        Table.AddColumn("[bold]System[/]");
    }

    public void Render(IEnumerable<TableRowMarkup> rows, int selectedIndex)
    {
        Table.Rows.Clear();

        var i = 0;
        foreach (var row in rows)
        {
            var marker = row.CanExpand
                ? (i == selectedIndex ? row.Expanded : row.Unexpanded)
                : row.Expanded;

            var name = i == selectedIndex ? row.SelectedName : row.UnselectedName;
            var state = i == selectedIndex ? row.SelectedState : row.UnselectedState;
            var system = i == selectedIndex ? row.SelectedSystem : row.UnselectedSystem;

            Table.AddRow(marker, name, state, system);
            i++;
        }
    }
    
    public void Resize(int newWidth)
    {
        Table.Width(newWidth);
    }
}