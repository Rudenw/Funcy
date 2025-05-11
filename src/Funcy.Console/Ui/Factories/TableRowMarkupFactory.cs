using Funcy.Console.Models;
using Funcy.Infrastructure.Model;
using Spectre.Console;

namespace Funcy.Console.Ui.Factories;

public static class TableRowMarkupFactory
{
    public static TableRowMarkup Create(FunctionAppDetails app)
    {
        var canExpand = app.Functions.Any();
        var tableRowMarkup = new TableRowMarkup
        {
            CanExpand = canExpand,
            Expanded = new Markup(canExpand ? "▾" : " "),
            SelectedName = new Markup(app.Name, new Style(Color.Black, Color.Yellow)),
            SelectedState = new Markup(app.State, new Style(Color.Black, Color.Yellow)),
            SelectedSystem = new Markup(app.System, new Style(Color.Black, Color.Yellow)),
            Unexpanded = new Markup(canExpand ? "▸" : " "),
            UnselectedName = new Markup(app.Name),
            UnselectedState = new Markup(app.State, new Style(UiHelper.GetStatusColor(app.State), decoration: Decoration.Bold)),
            UnselectedSystem = new Markup(app.System)
        };
        return tableRowMarkup;
        
    }
}