using Funcy.Console.Data;
using Funcy.Console.Models;
using Funcy.Console.Ui.Factories;
using Funcy.Console.Ui.Pagination;
using Funcy.Console.Ui.Renderers;
using Funcy.Infrastructure.Model;
using Spectre.Console;

namespace Funcy.Console.Ui.Panels;

public class FunctionAppPanel : IPanelController
{
    private readonly Dictionary<string, TableRowMarkup> _markupCache = [];
    private List<TableRowMarkup> _visibleRows = [];
    
    public Panel Panel { get; private set; }
    
    private readonly FunctionAppDataStore _dataStore;
    private readonly FunctionAppPaginator _paginator;
    private readonly FunctionAppTableRenderer _renderer;

    public FunctionAppPanel(List<FunctionAppDetails> functionAppDetails)
    {
        _dataStore = new FunctionAppDataStore();
        _paginator = new FunctionAppPaginator();
        _renderer = new FunctionAppTableRenderer();
        Panel = new Panel(_renderer.Table)
            .Header("Azure Function Apps", Justify.Center)
            .BorderColor(Color.Orange1);
        
        UpdateData(functionAppDetails);
    }
    
    public void UpdateData(List<FunctionAppDetails> functionAppDetails)
    {
        _dataStore.UpdateData(functionAppDetails);
        _paginator.OnDataUpdated(functionAppDetails);
        BuildCache(functionAppDetails);
        RefreshView();
    }
    
    private void RefreshView()
    {
        _visibleRows = _dataStore.FunctionAppDetails
            .Skip(_paginator.VisibleStartIndex)
            .Take(_paginator.MaxVisibleRows)
            .Select(app => _markupCache[app.Name])
            .ToList();
        
        _renderer.Render(_visibleRows, _paginator.SelectedIndex);
    }
    
    private void BuildCache(IEnumerable<FunctionAppDetails> apps)
    {
        foreach (var app in apps)
        {
            _markupCache[app.Name] = TableRowMarkupFactory.Create(app);
        }
    }
    
    public void OnResize()
    {
        _paginator.UpdateMaxVisibleRows();
        RefreshView();
    }

    public void HandleInputAsync(ConsoleKey key)
    {
        var scrolled = key switch
        {
            ConsoleKey.UpArrow   => _paginator.MoveUp(),
            ConsoleKey.DownArrow => _paginator.MoveDown(),
            _                     => false
        };

        if (scrolled)
        {
            RefreshView();
        }
        else
        {
            _renderer.Render(_visibleRows, _paginator.SelectedIndex);
        }
    }
    
    // public void UpdateSelectedTableRow()
    // {
    //     if (Table.Rows.Count > _oldSelectedIndex)
    //     {
    //         Table.Rows.Update(_oldSelectedIndex, 1, _visibleRows[_oldSelectedIndex].UnselectedName);
    //         Table.Rows.Update(_oldSelectedIndex, 2, _visibleRows[_oldSelectedIndex].UnselectedState);
    //         Table.Rows.Update(_oldSelectedIndex, 3, _visibleRows[_oldSelectedIndex].UnselectedSystem);
    //     }
    //
    //     if (Table.Rows.Count > _selectedIndex)
    //     {
    //         Table.Rows.Update(_selectedIndex, 1, _visibleRows[_selectedIndex].SelectedName);
    //         Table.Rows.Update(_selectedIndex, 2, _visibleRows[_selectedIndex].SelectedState);
    //         Table.Rows.Update(_selectedIndex, 3, _visibleRows[_selectedIndex].SelectedSystem);
    //     }
    // }
}