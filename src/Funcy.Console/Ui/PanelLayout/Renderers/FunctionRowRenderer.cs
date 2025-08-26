using Funcy.Core.Model;
using Spectre.Console;

namespace Funcy.Console.Ui.PanelLayout.Renderers;

public class FunctionLayoutRenderer: ILayoutRenderer<FunctionDetails>
{
    public RowMarkup CreateRowMarkup(FunctionDetails item)
    {
        var rowMarkup = new RowMarkup
        {
            Key = item.Key
        };
        rowMarkup.Add("Name", new RowCell(UiStyles.CreateSelectedCell(item.Name), new Markup(item.Name)));
        rowMarkup.Add("Trigger", new RowCell(UiStyles.CreateSelectedCell(item.Trigger), new Markup(item.Trigger)));
        
        return rowMarkup;
    }

    public ColumnLayout CreateColumnLayout()
    {
        return new ColumnLayout(new Column("Name"), new Column("Trigger"));
    }
}