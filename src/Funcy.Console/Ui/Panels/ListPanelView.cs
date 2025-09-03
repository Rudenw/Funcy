using Funcy.Console.Ui.Input;
using Funcy.Console.Ui.Navigation;
using Funcy.Console.Ui.Pagination;
using Funcy.Console.Ui.Pagination.Matchers;
using Funcy.Console.Ui.PanelLayout;
using Funcy.Console.Ui.PanelLayout.Renderers;
using Funcy.Console.Ui.Panels.Interfaces;
using Funcy.Console.Ui.Renderers;
using Funcy.Console.Ui.Shortcuts;
using Funcy.Core.Model;
using Spectre.Console;

namespace Funcy.Console.Ui.Panels;

public class ListPanelView<T> : IActionHandlingPanel, IListPanelView<T> where T : IComparable<T>, IHasKey
{
    private readonly ISearchMatcher<T> _searchMatcher;
    private readonly ILayoutRenderer<T> _layoutRenderer;
    private readonly IShortcutProvider<T> _shortcuts;
    private readonly Func<T, NavigationRequest?>? _onEnterNavigation;
    private readonly Func<T, NavigationRequest?>? _onActionNavigation;
    private readonly Func<FunctionAction, T, InputActionResult?>? _onAction; 

    private readonly Dictionary<string, RowMarkup> _markupCache = [];
    private List<RowMarkup> _visibleRows = [];
    
    public Panel Panel { get; }
    
    private readonly ListPanelPaginator _paginator;
    private readonly ListPanelTableRenderer _renderer;
    private string _searchText = "";
    private IReadOnlyList<T> _snapshot = [];
    private Dictionary<string, T> _itemIndex = new();


    public ListPanelView(IReadOnlyList<T> listObjects, ISearchMatcher<T> searchMatcher,
        ILayoutRenderer<T> layoutRenderer, IShortcutProvider<T> shortcuts, Func<T, NavigationRequest>? onEnterNavigation, string header,
        Func<FunctionAction, T, InputActionResult?>? onAction, Func<T, NavigationRequest>? onActionNavigation)

    {
        _searchMatcher = searchMatcher;
        _layoutRenderer = layoutRenderer;
        _shortcuts = shortcuts;
        _onEnterNavigation = onEnterNavigation;
        _onAction = onAction;
        _onActionNavigation = onActionNavigation;
        _paginator = new ListPanelPaginator();
        _renderer = new ListPanelTableRenderer(_layoutRenderer.CreateColumnLayout());
        Panel = new Panel(_renderer.Table)
            .Header(header, Justify.Center)
            .BorderColor(Color.Orange1);
        SetItems(listObjects);
    }
    
    public void SetItems(IReadOnlyList<T> items)
    {
        _snapshot = items;
        _paginator.UpdateTotalRows(items.Count);
        _itemIndex = items.ToDictionary(x => x.Key);
        BuildCache();
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

    private T? GetSelectedItem()
    {
        if (_visibleRows.Count == 0)
        {
            return default;
        }
        var selectedItemKey = _visibleRows[_paginator.SelectedIndex].Key;
        _itemIndex.TryGetValue(selectedItemKey, out var item);
        return item;
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
            .Select(app => new { App = app, IsMatch = _searchMatcher.TryMatch(app, _searchText) })
            .Where(x => x.IsMatch)
            .ToList();
        
        var skip = filtered.Count < _paginator.MaxVisibleRows ? 0 : _paginator.VisibleStartIndex; //kanske kan skippas och vi kör _paginator.VisibleStartIndex bara enligt gippy

        return (
            filtered.Skip(skip).Take(_paginator.MaxVisibleRows).Select(x => x.App),
            filtered.Count
        );
    }
    
    private void BuildCache()
    {
        foreach (var app in _snapshot)
        {
            _markupCache[app.Key] = _layoutRenderer.CreateRowMarkup(app);
        }
    }
    public bool TryGetNavigationRequest(out NavigationRequest? navigationRequest)
    {
        navigationRequest = null;
        if (_onEnterNavigation is null)
        {
            return false;
        }

        var selectedItem = GetSelectedItem();
        ArgumentNullException.ThrowIfNull(selectedItem);
        navigationRequest = _onEnterNavigation(selectedItem);
        return navigationRequest is not null;
    }
    
    public bool TryGetActionNavigationRequest(out NavigationRequest? navigationRequest)
    {
        navigationRequest = null;
        if (_onActionNavigation is null)
        {
            return false;
        }

        var selectedItem = GetSelectedItem();
        ArgumentNullException.ThrowIfNull(selectedItem);

        navigationRequest = _onActionNavigation(selectedItem);
        return navigationRequest is not null;
    }

    public Dictionary<TableIndex, ShortcutMap> GetShortcuts()
    {
        return _shortcuts.Describe(GetSelectedItem());
    }

    public bool TryBuildAction(FunctionAction action, out InputActionResult? result)
    {
        result = null;
        if (_onAction is null)
            return false;

        var selected = GetSelectedItem();
        if (selected is null)
            return false;

        var built = _onAction(action, selected);
        if (built is null)
            return false;

        result = built;
        return true;
    }


}