using Funcy.Core.Model;
using Spectre.Console;

namespace Funcy.Console.Ui.PanelLayout.Renderers;

public class SubscriptionLayoutRenderer: ILayoutRenderer<SubscriptionDetails>
{
    public RowMarkup CreateRowMarkup(SubscriptionDetails item)
    {
        var rowMarkup = new RowMarkup
        {
            Key = item.Key
        };
        rowMarkup.Add("Name", new RowCell(UiStyles.CreateSelectedCell(item.Name), new Markup(item.Name)));
        rowMarkup.Add("Current",
            new RowCell(UiStyles.CreateSelectedCell(item.Current.ToString()), new Markup(item.Current.ToString())));
        
        return rowMarkup;
    }

    public ColumnLayout<SubscriptionDetails> CreateColumnLayout()
    {
        return new ColumnLayout<SubscriptionDetails>(new Column<SubscriptionDetails>("Name", (f) => f.Name),
            new Column<SubscriptionDetails>("Current", f => f.Current));
    }
}