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
        
        return rowMarkup;
    }

    public ColumnLayout CreateColumnLayout()
    {
        return new ColumnLayout(new Column("Name"), new Column("System"), new Column("State"), new Column("Status"));
    }
}