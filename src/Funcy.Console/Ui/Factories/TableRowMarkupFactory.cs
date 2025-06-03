using Spectre.Console;
using Funcy.Console.Ui;
using Funcy.Console.Ui.Factories.Models;
using Funcy.Core.Model;

namespace Funcy.Console.Ui.Factories;

public static class TableRowMarkupFactory
{
    public static TableRowMarkup Create(FunctionAppDetails app)
    {
        var canExpand = app.Functions.Any();
        var tableRowMarkup = new TableRowMarkup
        {
            CanExpand = canExpand,
            SelectedExpanded = new Markup(canExpand ? "▾" : " "),
            SelectedUnexpanded = UiStyles.CreateSelectedCell(canExpand ? "▸" : " "),
            SelectedName = UiStyles.CreateSelectedCell(app.Name),
            SelectedState = UiStyles.CreateSelectedCell(app.State),
            SelectedSystem = UiStyles.CreateSelectedCell(app.System),
            UnselectedExpanded = new Markup(canExpand ? "▾" : " "),
            UnselectedUnexpanded = new Markup(canExpand ? "▸" : " "),
            UnselectedName = new Markup(app.Name),
            UnselectedState = UiStyles.CreateStatusCell(app.State),
            UnselectedSystem = new Markup(app.System),
            FunctionAppDetails = app
        };
        return tableRowMarkup;
        
    }
    
    public static TableRowMarkup Create(DeploymentSlotDetails slotDetails)
    {
        var tableRowMarkup = new TableRowMarkup
        {
            CanExpand = false,
            SelectedExpanded = new Markup(""),
            SelectedUnexpanded = UiStyles.CreateSelectedCell(""),
            SelectedName = UiStyles.CreateSelectedCell(slotDetails.Name),
            SelectedState = UiStyles.CreateSelectedCell(slotDetails.State),
            SelectedSystem = new Markup(""),
            UnselectedExpanded = new Markup(""),
            UnselectedUnexpanded = new Markup(""),
            UnselectedName = new Markup(slotDetails.Name),
            UnselectedState = UiStyles.CreateStatusCell(slotDetails.State),
            UnselectedSystem = new Markup(""),
            FunctionAppDetails = null!,
            
        };
        return tableRowMarkup;
        
    }
}