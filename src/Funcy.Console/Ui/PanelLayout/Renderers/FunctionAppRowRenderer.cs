using Funcy.Core.Model;
using Spectre.Console;

namespace Funcy.Console.Ui.PanelLayout.Renderers;

public class FunctionAppLayoutRenderer: ILayoutRenderer<FunctionAppDetails>
{
    public RowMarkup CreateRowMarkup(FunctionAppDetails item)
    {
        var rowMarkup = new RowMarkup
        {
            Key = item.Key
        };
        rowMarkup.Add("Name", new RowCell(UiStyles.CreateSelectedCell(item.Name), new Markup(item.Name)));
        rowMarkup.Add("System", new RowCell(UiStyles.CreateSelectedCell(item.System), new Markup(item.System)));
        rowMarkup.Add("State", new RowCell(UiStyles.CreateSelectedCell(item.State.ToDisplayLabel()), UiStyles.CreateStateCell(item.State)));
        rowMarkup.Add("Status", new RowCell(UiStyles.CreateSelectedCell(item.Status.ToDisplayLabel()), UiStyles.CreateStatusCell(item.Status)));
        rowMarkup.Add("", new RowCell(UiStyles.CreateSelectedCell(item.AnimatingFrame), new Markup(item.AnimatingFrame)));
        
        return rowMarkup;
    }

    public ColumnLayout<FunctionAppDetails> CreateColumnLayout()
    {
        return new ColumnLayout<FunctionAppDetails>(new Column<FunctionAppDetails>("Name", f => f.Name, 40),
            new Column<FunctionAppDetails>("System", f => f.System, 10),
            new Column<FunctionAppDetails>("State", f => f.State, 10),
            new Column<FunctionAppDetails>("Status", f => f.Status.ToDisplayLabel(), 20),
            new Column<FunctionAppDetails>("", null, 10, true));
    }
}