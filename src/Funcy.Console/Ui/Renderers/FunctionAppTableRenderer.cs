using Spectre.Console;
using Funcy.Console.Ui;
using Funcy.Console.Ui.Factories.Models;
using Spectre.Console.Rendering;

namespace Funcy.Console.Ui.Renderers;

public class FunctionAppTableRenderer
{
    private readonly List<(Func<TableRowMarkup, bool, Markup> selector, string columnName)> _columns;
    public Table Table { get; set; }
    
    public FunctionAppTableRenderer(List<(Func<TableRowMarkup, bool, Markup> selector, string columnName)> columns, int width = 100)
    {
        Table = new Table();
        Table.Border(TableBorder.None);
        Table.Width(width);
        _columns = columns;
        foreach (var column in columns)
        {
            Table.AddColumn(UiStyles.CreateHeaderText(column.columnName));
        }
    }

    public void Render(IEnumerable<TableRowMarkup> rows, int selectedIndex)
    {
        Table.Rows.Clear();

        var i = 0;
        foreach (var row in rows)
        {
            List<IRenderable> markupsToRender = [];
            foreach (var columnFunc in _columns)
            {
                markupsToRender.Add(columnFunc.selector(row, i == selectedIndex));
            }

            Table.AddRow(markupsToRender);
            i++;
        }
    }
}