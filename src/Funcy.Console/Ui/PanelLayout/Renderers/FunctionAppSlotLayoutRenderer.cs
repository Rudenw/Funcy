using Funcy.Core.Model;
using Spectre.Console;

namespace Funcy.Console.Ui.PanelLayout.Renderers;

public class FunctionAppSlotLayoutRenderer: ILayoutRenderer<FunctionAppSlotDetails>
{
    public RowMarkup CreateRowMarkup(FunctionAppSlotDetails item)
    {
        var rowMarkup = new RowMarkup
        {
            Key = item.Key
        };
        rowMarkup.Add("Name", new RowCell(UiStyles.CreateSelectedCell(item.Name), new Markup(item.Name)));
        rowMarkup.Add("State", new RowCell(UiStyles.CreateSelectedCell(item.State.ToDisplayLabel()), UiStyles.CreateStatusCell(item.State.ToDisplayLabel())));
        
        return rowMarkup;
    }

    public ColumnLayout CreateColumnLayout()
    {
        return new ColumnLayout(new Column("Name"), new Column("State"));
    }
}