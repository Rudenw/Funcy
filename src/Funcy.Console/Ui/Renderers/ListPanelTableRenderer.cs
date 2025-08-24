using Spectre.Console;
using Funcy.Console.Ui.PanelLayout;
using Spectre.Console.Rendering;

namespace Funcy.Console.Ui.Renderers;

public class ListPanelTableRenderer
{
    private readonly IReadOnlyList<Column> _columns;
    public Table Table { get; set; }
    
    public ListPanelTableRenderer(ColumnLayout columnLayout, int width = 100)
    {
        Table = new Table();
        Table.Border(TableBorder.None);
        Table.Width(width);
        _columns = columnLayout.Columns;
        foreach (var column in _columns)
        {
            Table.AddColumn(UiStyles.CreateHeaderText(column.Header));
        }
    }

    public void Render(IEnumerable<RowMarkup> rows, int selectedIndex)
    {
        Table.Rows.Clear();

        var i = 0;
        foreach (var row in rows)
        {
            List<IRenderable> markupsToRender = [];
            foreach (var column in _columns)
            {
                
                markupsToRender.Add(row.GetCell(column.Header, i == selectedIndex));
            }

            Table.AddRow(markupsToRender);
            i++;
        }
    }
}