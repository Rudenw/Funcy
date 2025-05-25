using Funcy.Console.Data;
using Funcy.Console.Ui.Factories;
using Funcy.Console.Ui.Factories.Models;
using Funcy.Console.Ui.Pagination;
using Funcy.Console.Ui.Renderers;
using Funcy.Core.Model;
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
    private readonly Lock _lock = new();
    private string _searchText = "";

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
        _paginator.UpdateTotalRows(_dataStore.FunctionAppDetails.Count);
        BuildCache(_dataStore.FunctionAppDetails);
        RefreshView();
    }
    
    public void UpdatePartialData(List<FunctionAppDetails> functionAppDetails)
    {
        _dataStore.UpdatePartialData(functionAppDetails);
        _paginator.UpdateTotalRows(_dataStore.FunctionAppDetails.Count);
        BuildCache(_dataStore.FunctionAppDetails);
        RefreshView();
    }
    
    public void RemoveFunctionApps(List<FunctionAppDetails> removed)
    {
        _dataStore.RemoveFunctionApps(removed);
        _paginator.UpdateTotalRows(_dataStore.FunctionAppDetails.Count);
        BuildCache(_dataStore.FunctionAppDetails);
        RefreshView();
    }
    
    public void HandleResize()
    {
        _paginator.UpdateMaxVisibleRows();
        RefreshView();
    }

    public void HandleInput(ConsoleKeyInfo keyInfo)
    {
        var scrolled = keyInfo.Key switch
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
    
    public void SetSearchText(string searchText)
    {
        _searchText = searchText.Trim();
        RefreshView();
    }

    public FunctionAppDetails GetSelectedFunctionAppDetails()
    {
        var selectedFunctionApp = _dataStore.FunctionAppDetails[_paginator.VisibleStartIndex + _paginator.SelectedIndex];
        return selectedFunctionApp;
    }
    
    private void RefreshView()
    {
        var (appsToShow, totalCount) = GetVisibleFunctionApps();

        _visibleRows = appsToShow
            .Select(app => _markupCache[app.Name])
            .ToList();

        _paginator.UpdateTotalRows(totalCount);
        _renderer.Render(_visibleRows, _paginator.SelectedIndex);
    }
    
    private (IEnumerable<FunctionAppDetails> appsToShow, int totalCount) GetVisibleFunctionApps()
    {
        if (string.IsNullOrWhiteSpace(_searchText))
        {
            var all = _dataStore.FunctionAppDetails;
            return (
                all.Skip(_paginator.VisibleStartIndex).Take(_paginator.MaxVisibleRows),
                all.Count
            );
        }

        var filtered = _dataStore.FunctionAppDetails
            .Select(app => new { App = app, Match = FunctionAppMatcher.Match(app, _searchText) })
            .Where(x => x.Match.IsMatch)
            .ToList();
        
        var skip = filtered.Count < _paginator.MaxVisibleRows ? 0 : _paginator.VisibleStartIndex;

        return (
            filtered.Skip(skip).Take(_paginator.MaxVisibleRows).Select(x => x.App),
            filtered.Count
        );
    }
    
    private void BuildCache(IEnumerable<FunctionAppDetails> apps)
    {
        foreach (var app in apps)
        {
            _markupCache[app.Name] = TableRowMarkupFactory.Create(app);
        }
    }
}