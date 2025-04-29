using Funcy.Console.Models;
using Funcy.Infrastructure.Model;
using Spectre.Console;

namespace Funcy.Console.Ui;

public class TableRowUpdater
{
    private readonly List<TableRowMarkup> _allRows = [];
    private readonly List<TableRowMarkup> _visibleRows = [];
    
    public void UpdateVisibleTableRows(Table table, int visibleStart, int maxVisibleRows)
    {
        table.Rows.Clear();
        _visibleRows.Clear();

        foreach (var row in _allRows.Skip(visibleStart).Take(maxVisibleRows))
        {
            table.AddRow(row.Columns);
            _visibleRows.Add(row);
        }
    }

    public void UpdateSelectedTableRow(TableRowCollection tableRows, int oldSelectedIndex, int selectedIndex)
    {
        if (tableRows.Count > oldSelectedIndex)
        {
            tableRows.Update(oldSelectedIndex, 0, _visibleRows[oldSelectedIndex].UnselectedName);
            tableRows.Update(oldSelectedIndex, 1, _visibleRows[oldSelectedIndex].UnselectedState);
            tableRows.Update(oldSelectedIndex, 2, _visibleRows[oldSelectedIndex].UnselectedSystem);
        }

        if (tableRows.Count > selectedIndex)
        {
            tableRows.Update(selectedIndex, 0, _visibleRows[selectedIndex].SelectedName);
            tableRows.Update(selectedIndex, 1, _visibleRows[selectedIndex].SelectedState);
            tableRows.Update(selectedIndex, 2, _visibleRows[selectedIndex].SelectedSystem);
        }
    }

    private Color GetStatusColor(string state)
    {
        return state == "Running" ? Color.Green : Color.Red;
    }

    public void AddToAllRows(TableRowMarkup tableRowMarkup)
    {
        _allRows.Add(tableRowMarkup);
    }

    public void AddToVisibleRows(TableRowMarkup tableRowMarkup)
    {
        _visibleRows.Add(tableRowMarkup);
    }
}