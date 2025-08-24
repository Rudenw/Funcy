using Funcy.Console.Ui.Pagination;
using Funcy.Console.Ui.PanelLayout;
using Funcy.Console.Ui.PanelLayout.Renderers;
using Funcy.Console.Ui.Renderers;
using Funcy.Core.Model;
using Spectre.Console;

namespace Funcy.Console.Ui.Panels.GenericTestPanel;

public class ListPanel<T> : IListPanel where T : IComparable<T>, IHasKey
{
    private readonly ISearchMatcher<T> _searchMatcher;
    private readonly ILayoutRenderer<T> _layoutRenderer;
    private readonly Dictionary<string, RowMarkup> _markupCache = [];
    private List<RowMarkup> _visibleRows = [];
    
    public Panel Panel { get; }
    
    private readonly ListPanelDataStore<T> _dataStore;
    private readonly ListPanelPaginator _paginator;
    private readonly ListPanelTableRenderer _renderer;
    private string _searchText = "";
    private IReadOnlyList<T> _snapshot = [];

    public ListPanel(List<T> listObjects, ISearchMatcher<T> searchMatcher, ILayoutRenderer<T> layoutRenderer, string header)
    {
        _searchMatcher = searchMatcher;
        _layoutRenderer = layoutRenderer;
        _dataStore = new ListPanelDataStore<T>();
        _paginator = new ListPanelPaginator();
        _renderer = new ListPanelTableRenderer(_layoutRenderer.CreateColumnLayout());
        Panel = new Panel(_renderer.Table)
            .Header(header, Justify.Center)
            .BorderColor(Color.Orange1);
        UpdateData(listObjects);
    }
    
    public void UpdateData(List<T> listObjects)
    {
        _dataStore.UpdateAll(listObjects);
        _snapshot = _dataStore.Snapshot();
        _paginator.UpdateTotalRows(_dataStore.Count);
        BuildCache(_dataStore.Snapshot());
        RefreshView();
    }
    
    public void UpdatePartialData(List<T> listObjects)
    {
        _dataStore.UpsertMany(listObjects);
        _snapshot = _dataStore.Snapshot();
        _paginator.UpdateTotalRows(_dataStore.Count);
        BuildCache(listObjects);
        RefreshView();
    }
    
    public void RemoveItems(List<T> removed)
    {
        _dataStore.RemoveMany(removed);
        _snapshot = _dataStore.Snapshot();
        _paginator.UpdateTotalRows(_dataStore.Count);
        BuildCache(_dataStore.Snapshot());
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
        if (!_searchText.Equals(searchText.Trim()))
        {
            _searchText = searchText.Trim();
            RefreshView();    
        }
    }

    public T? GetSelectedItem()
    {
        var selectedItemKey = _visibleRows[_paginator.SelectedIndex].Key;
        return _dataStore.TryGet(selectedItemKey);
    }
    
    private void RefreshView()
    {
        var (appsToShow, totalCount) = GetVisibleItems();

        _visibleRows = appsToShow
            .Select(app => _markupCache[app.Key])
            .ToList();

        _paginator.UpdateTotalRows(totalCount);
        _renderer.Render(_visibleRows, _paginator.SelectedIndex);
    }
    
    private (IEnumerable<T> appsToShow, int totalCount) GetVisibleItems()
    {
        if (string.IsNullOrWhiteSpace(_searchText))
        { 
            return (
                _snapshot.Skip(_paginator.VisibleStartIndex).Take(_paginator.MaxVisibleRows),
                _snapshot.Count
            );
        }

        var filtered = _snapshot
            .Select(app => new { App = app, IsMatch = _searchMatcher.TryMatch(app, _searchText) }) //Rewrite to generic, but not now
            .Where(x => x.IsMatch)
            .ToList();
        
        var skip = filtered.Count < _paginator.MaxVisibleRows ? 0 : _paginator.VisibleStartIndex; //kanske kan skippas och vi kör _paginator.VisibleStartIndex bara enligt gippy

        return (
            filtered.Skip(skip).Take(_paginator.MaxVisibleRows).Select(x => x.App),
            filtered.Count
        );
    }
    
    private void BuildCache(IReadOnlyList<T> apps)
    {
        foreach (var app in apps)
        {
            _markupCache[app.Key] = _layoutRenderer.CreateRowMarkup(app);
        }
    }
}

public interface IListPanel
{
    void HandleResize();
    Panel Panel { get; }
    void HandleInput(ConsoleKeyInfo keyInfo);
}