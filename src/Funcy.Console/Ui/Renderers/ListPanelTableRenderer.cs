using Spectre.Console;
using Funcy.Console.Ui.PanelLayout;
using Spectre.Console.Rendering;

namespace Funcy.Console.Ui.Renderers;

public class ListPanelTableRenderer<T>
{
    private readonly IReadOnlyList<Column<T>> _columns;
    public Table Table { get; set; }
    
    public ListPanelTableRenderer(ColumnLayout<T> columnLayout, int width = 110)
    {
        Table = new Table();
        Table.Border(TableBorder.None);
        Table.Width(width);
        _columns = columnLayout.Columns;
        var index = 1;
        foreach (var column in _columns)
        {
            var tableColumn = new TableColumn(UiStyles.CreateHeaderText(column.Header, index++, false));
            if (column.Width > 0)
            {
                tableColumn.Width(column.Width);
            }

            Table.AddColumn(tableColumn);
        }
    }

    public void ToggleSortingColumn(int? columnIndex, bool descending)
    {
        var index = 0;
        foreach (var column in _columns)
        {
            Table.Columns[index].Header(UiStyles.CreateHeaderText(column.Header, index + 1, descending,
                columnIndex is not null && columnIndex.Value == index + 1));
            index++;
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