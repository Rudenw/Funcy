using Funcy.Console.Models;
using Funcy.Infrastructure.Model;
using Spectre.Console;
using Funcy.Console.Ui;

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
            SelectedName = UiStyles.CreateSelectedCell(app.Name),
            SelectedState = UiStyles.CreateSelectedCell(app.State),
            SelectedSystem = UiStyles.CreateSelectedCell(app.System),
            Unexpanded = new Markup(canExpand ? "▸" : " "),
            UnselectedName = new Markup(app.Name),
            UnselectedState = UiStyles.CreateStatusCell(app.State),
            UnselectedSystem = new Markup(app.System)
        };
        return tableRowMarkup;
        
    }
}