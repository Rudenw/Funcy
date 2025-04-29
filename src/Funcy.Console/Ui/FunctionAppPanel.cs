using Funcy.Console.Models;
using Funcy.Infrastructure.Model;
using Spectre.Console;

namespace Funcy.Console.Ui;

public class FunctionAppPanel
{
    public Panel Panel { get; private set; }
    public Table Table { get; private set; }
    
    public int MaxVisibleRows { get; private set; } = 5;
    private readonly List<FunctionAppDetails> _functionAppDetails;
    private readonly TableRowUpdater _tableRowUpdater;

    public FunctionAppPanel(List<FunctionAppDetails> functionAppDetails, TableRowUpdater tableRowUpdater)
    {
        _functionAppDetails = functionAppDetails;
        _tableRowUpdater = tableRowUpdater;

        UpdateMaxVisibleRows();
        CreateFunctionAppPanel();
    }

    public void UpdateMaxVisibleRows()
    {
        MaxVisibleRows = Math.Min(System.Console.WindowHeight - 6, _functionAppDetails.Count);
    }

    public Panel CreateFunctionAppPanel()
    {
        Table = CreateFunctionAppTable(_functionAppDetails);
        Panel = new Panel(Table)
            .Header("Azure Function Apps", Justify.Center)
            .BorderColor(Color.Orange1);

        return Panel;
    }
    
    private Table CreateFunctionAppTable(List<FunctionAppDetails> functionAppDetails)
    {
        var table = new Table();
        table.AddColumn("[bold]Name[/]");
        table.AddColumn("[bold]Status[/]");
        table.AddColumn("[bold]System[/]");

        for (var i = 0; i < functionAppDetails.Count; i++)
        {
            var app = functionAppDetails[i];

            var tableRowMarkup = new TableRowMarkup
            {
                SelectedName = new Markup($"[black on yellow]{app.Name}[/]"),
                SelectedState = new Markup($"[black on yellow bold]{app.State}[/]"),
                SelectedSystem = new Markup($"[black on yellow]{app.System}[/]"),
                UnselectedName = new Markup($"{app.Name}"),
                UnselectedState = new Markup($"[bold {UiHelper.GetStatusColor(app.State)}]{app.State}[/]"),
                UnselectedSystem = new Markup($"{app.System}")
            };

            _tableRowUpdater.AddToAllRows(tableRowMarkup);

            if (i < MaxVisibleRows)
            {
                table.AddRow(tableRowMarkup.Columns);
                _tableRowUpdater.AddToVisibleRows(tableRowMarkup);
            }
        }

        return table;
    }
}