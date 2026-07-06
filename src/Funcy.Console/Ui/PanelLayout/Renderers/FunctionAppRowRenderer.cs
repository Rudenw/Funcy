using Funcy.Core.Model;
using Spectre.Console;

namespace Funcy.Console.Ui.PanelLayout.Renderers;

public class FunctionAppLayoutRenderer(IReadOnlyList<string> tagColumns, Func<string, int> getColumnWidth) : ILayoutRenderer<FunctionAppDetails>
{
    public RowMarkup CreateRowMarkup(FunctionAppDetails item)
    {
        var rowMarkup = new RowMarkup
        {
            Key = item.Key
        };
        // Pinned apps get a glyph prefix; the Name sort key stays item.Name so ordering is unaffected.
        var displayName = item.IsPinned ? $"{UiStyles.PinGlyph} {item.Name}" : item.Name;
        var unselectedName = item.IsPinned ? $"[{UiStyles.Sort}]{UiStyles.PinGlyph}[/] {item.Name}" : item.Name;
        rowMarkup.Add("Name", new RowCell(UiStyles.CreateSelectedCell(displayName), new Markup(unselectedName)));

        foreach (var tag in tagColumns)
        {
            var value = item.Tags.TryGetValue(tag, out var v) ? v : string.Empty;
            rowMarkup.Add(tag, new RowCell(UiStyles.CreateSelectedCell(value), new Markup(value)));
        }

        rowMarkup.Add("State", new RowCell(UiStyles.CreateSelectedCell(item.State.ToDisplayLabel()), UiStyles.CreateStateCell(item.State)));
        rowMarkup.Add("Status", new RowCell(UiStyles.CreateSelectedCell(item.Status.ToDisplayLabel()), UiStyles.CreateStatusCell(item.Status)));
        rowMarkup.Add("", new RowCell(UiStyles.CreateSelectedCell(item.AnimatingFrame), new Markup(item.AnimatingFrame)));
        
        return rowMarkup;
    }

    public ColumnLayout<FunctionAppDetails> CreateColumnLayout()
    {
        var columns = new List<Column<FunctionAppDetails>>
        {
            new("Name", f => f.Name, 40)
        };

        foreach (var tag in tagColumns)
        {
            var tagCopy = tag;
            columns.Add(new Column<FunctionAppDetails>(tagCopy, f => f.Tags.TryGetValue(tagCopy, out var v) ? v : null, getColumnWidth(tagCopy)));
        }

        columns.Add(new Column<FunctionAppDetails>("State", f => f.State, 10));
        columns.Add(new Column<FunctionAppDetails>("Status", f => f.Status.ToDisplayLabel(), 20));
        columns.Add(new Column<FunctionAppDetails>("", null, 10, true));

        return new ColumnLayout<FunctionAppDetails>([.. columns]);
    }
}
