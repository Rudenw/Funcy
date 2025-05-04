using Funcy.Console.Models;
using Funcy.Infrastructure.Model;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Funcy.Console.Ui.Panels;

public class FunctionAppPanel : IPanelController
{
    private List<TableRowMarkup> _allRows = [];
    private List<TableRowMarkup> _visibleRows = [];

    private int _selectedIndex = 0;
    private int _visibleStartIndex = 0;
    
    public Panel Panel { get; private set; }
    private Table Table { get; set; }

    private int MaxVisibleRows { get; set; } = 5;
    private List<FunctionAppDetails> FunctionAppDetails { get; set; }
    private int _oldSelectedIndex;

    public FunctionAppPanel(List<FunctionAppDetails> functionAppDetails)
    {
        FunctionAppDetails = functionAppDetails;

        UpdateMaxVisibleRows();
        
        Table = CreateFunctionAppTable(FunctionAppDetails);
        Panel = new Panel(Table)
            .Header("Azure Function Apps", Justify.Center)
            .BorderColor(Color.Orange1);
    }
    
    public void OnResize()
    {
        UpdateMaxVisibleRows();
        CreateFunctionAppPanel();
        UpdateSelectedTableRow();
    }

    public async Task HandleInputAsync(ConsoleKey key)
    {
        bool isVisibleStartIndexChanged = false;
        _oldSelectedIndex = _selectedIndex;
        switch (key)
        {
            case ConsoleKey.UpArrow:
                _selectedIndex--;
                        
                if (_selectedIndex < 0 && _visibleStartIndex > 0)
                {
                    isVisibleStartIndexChanged = true;
                    _visibleStartIndex--;
                    _selectedIndex = 0;
                }

                if (_selectedIndex < 0)
                {
                    _selectedIndex = 0;
                }
                        
                break;
            case ConsoleKey.DownArrow:
                _selectedIndex++;

                if (_selectedIndex >= MaxVisibleRows && _selectedIndex + _visibleStartIndex < _allRows.Count)
                {
                    isVisibleStartIndexChanged = true;
                    _visibleStartIndex++;
                    _selectedIndex = MaxVisibleRows - 1;
                }

                if (_selectedIndex >= MaxVisibleRows)
                {
                    _selectedIndex = MaxVisibleRows - 1;
                }
                        
                break;
        }

        if (isVisibleStartIndexChanged)
        {
            UpdateVisibleTableRows();
        }
        //UpdateView();
    }

    private void UpdateMaxVisibleRows()
    {
        MaxVisibleRows = Math.Min(System.Console.WindowHeight - 10, FunctionAppDetails.Count);
    }

    public IRenderable CreateFunctionAppPanel()
    {
        Table = CreateFunctionAppTable(FunctionAppDetails);
        Panel = new Panel(Table)
            .Header("Azure Function Apps", Justify.Center)
            .BorderColor(Color.Orange1);

        UpdateSelectedTableRow();

        return Panel;
    }

    public void UpdateVisibleTableRows()
    {
        Table.Rows.Clear();
        _visibleRows.Clear();

        foreach (var row in _allRows.Skip(_visibleStartIndex).Take(MaxVisibleRows))
        {
            Table.AddRow(row.Columns);
            _visibleRows.Add(row);
        }
    }
    
    public void UpdateSelectedTableRow()
    {
        if (Table.Rows.Count > _oldSelectedIndex)
        {
            Table.Rows.Update(_oldSelectedIndex, 0, _visibleRows[_oldSelectedIndex].UnselectedName);
            Table.Rows.Update(_oldSelectedIndex, 1, _visibleRows[_oldSelectedIndex].UnselectedState);
            Table.Rows.Update(_oldSelectedIndex, 2, _visibleRows[_oldSelectedIndex].UnselectedSystem);
        }

        if (Table.Rows.Count > _selectedIndex)
        {
            Table.Rows.Update(_selectedIndex, 0, _visibleRows[_selectedIndex].SelectedName);
            Table.Rows.Update(_selectedIndex, 1, _visibleRows[_selectedIndex].SelectedState);
            Table.Rows.Update(_selectedIndex, 2, _visibleRows[_selectedIndex].SelectedSystem);
        }
    }
    
    private Table CreateFunctionAppTable(List<FunctionAppDetails> functionAppDetails)
    {
        var table = new Table();
        table.Border(TableBorder.None);
        table.Width(100);
        table.AddColumn("[bold]Name[/]");
        table.AddColumn("[bold]Status[/]");
        table.AddColumn("[bold]System[/]");

        for (var i = 0; i < functionAppDetails.Count; i++)
        {
            var app = functionAppDetails[i];

            var tableRowMarkup = new TableRowMarkup
            {
                SelectedName = new Markup(app.Name, new Style(Color.Black, Color.Yellow)),
                SelectedState = new Markup(app.State, new Style(Color.Black, Color.Yellow)),
                SelectedSystem = new Markup(app.System, new Style(Color.Black, Color.Yellow)),
                UnselectedName = new Markup(app.Name),
                UnselectedState = new Markup(app.State, new Style(UiHelper.GetStatusColor(app.State), decoration: Decoration.Bold)),
                UnselectedSystem = new Markup(app.System)
            };

            _allRows.Add(tableRowMarkup);

            if (i < MaxVisibleRows)
            {
                table.AddRow(tableRowMarkup.Columns);
                _visibleRows.Add(tableRowMarkup);
            }
        }

        return table;
    }
}