using Funcy.Console.Handlers;
using Funcy.Console.Handlers.Models;
using Funcy.Console.Ui.Input;
using Funcy.Console.Ui.Navigation;
using Funcy.Console.Ui.Pagination;
using Funcy.Console.Ui.Pagination.Matchers;
using Funcy.Console.Ui.Pagination.Sorters;
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
    private readonly ISorter<T> _sorter;
    private readonly ISearchMatcher<T> _searchMatcher;
    private readonly ILayoutRenderer<T> _layoutRenderer;
    private readonly IShortcutProvider<T> _shortcuts;
    private readonly IAnimationProvider _animationProvider;
    private readonly Func<T, NavigationRequest?>? _onEnterNavigation;
    private readonly Func<T, NavigationRequest?>? _onActionNavigation;
    private readonly Func<FunctionAction, T, InputActionResult?>? _onAction; 
    private readonly Func<UiStatusSnapshot, string?>? _emptyStateMessage;

    // Single source of truth for this panel, guarded by _gate. Items, their pre-rendered
    // markup, and the lazily-sorted view all live here — replacing the old controller-owned
    // ListPanelDataStore, the IReadOnlyList handoff, and a duplicate item index.
    private readonly Lock _gate = new();
    private readonly Dictionary<string, T> _items = new();
    private readonly Dictionary<string, RowMarkup> _markupCache = new(StringComparer.Ordinal);
    private List<T> _sortedCache = [];
    private bool _sortDirty = true;
    // Set by background model updates; consumed on the render thread in RenderIfNeeded so the
    // Spectre table is only ever written from one thread.
    private bool _needsRender;

    private List<RowMarkup> _visibleRows = [];
    // Pending header text and a dynamic empty-state override, set by background updates and
    // applied on the render thread. Both guarded by _gate.
    private string? _pendingHeader;
    private string? _dynamicEmptyState;

    // Number of leading rows in _visibleRows that are sticky (pinned off-screen active operations),
    // not part of the scrollable content. The selection index is relative to the content, so it is
    // shifted by this count when locating or highlighting the selected row. Touched only on the
    // render thread (RebuildVisibleRows / GetSelectedItem / RenderCurrentView).
    private int _stickyCount;

    public Panel Panel { get; }

    private readonly ListPanelPaginator _paginator;
    private readonly ListPanelTableRenderer<T> _renderer;
    private readonly ColumnLayout<T> _columnLayout;
    // Console width source, injectable for headless tests exactly like windowHeight.
    private readonly Func<int> _windowWidth;
    // Resolved table width currently applied; guards against redundant re-flow/cache clears.
    private int _tableWidth;
    private string _searchText = "";
    private UiStatusSnapshot _uiStatus;


    public ListPanelView(ISearchMatcher<T> searchMatcher,
        ILayoutRenderer<T> layoutRenderer, IShortcutProvider<T> shortcuts, IAnimationProvider animationProvider, Func<T, NavigationRequest>? onEnterNavigation, string header,
        Func<FunctionAction, T, InputActionResult?>? onAction, Func<T, NavigationRequest>? onActionNavigation,
        Func<UiStatusSnapshot, string?>? emptyStateMessage = null, Func<int>? windowHeight = null, Func<int>? windowWidth = null)

    {
        _searchMatcher = searchMatcher;
        _layoutRenderer = layoutRenderer;
        _shortcuts = shortcuts;
        _animationProvider = animationProvider;
        _onEnterNavigation = onEnterNavigation;
        _onAction = onAction;
        _onActionNavigation = onActionNavigation;
        _emptyStateMessage = emptyStateMessage;
        _windowWidth = windowWidth ?? (() => System.Console.WindowWidth);
        _paginator = new ListPanelPaginator(windowHeight);

        _columnLayout = _layoutRenderer.CreateColumnLayout();
        _renderer = new ListPanelTableRenderer<T>(_columnLayout);
        _sorter = new ListPanelSorter<T>(_columnLayout);

        Panel = new Panel(_renderer.Table)
        {
            Width = AdaptiveLayout.PanelWidth(AdaptiveLayout.MinTableWidth)
        }
            .Header(header, Justify.Center)
            .BorderColor(Color.Orange1);

        // Size to the actual terminal before the first render so startup is already adaptive.
        ApplyAdaptiveWidth();
    }

    // Resolves the target table width from the console, and — when it changed — re-flows the
    // table columns, tells the layout renderer the new widths (so cell truncation matches what is
    // on screen), rebuilds the per-item markup cache at the new widths, and resizes the panel.
    // Runs on the render thread (ctor or HandleResize), the only place allowed to touch the table.
    private void ApplyAdaptiveWidth()
    {
        var target = AdaptiveLayout.ResolveTableWidth(_windowWidth());
        if (target == _tableWidth)
        {
            return;
        }

        _tableWidth = target;
        _renderer.ApplyWidth(target);

        var resolved = _columnLayout.Resolve(target);
        var byHeader = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < _columnLayout.Columns.Count; i++)
        {
            byHeader[_columnLayout.Columns[i].Header] = resolved[i];
        }

        _layoutRenderer.SetResolvedWidths(byHeader);

        lock (_gate)
        {
            // Cached markup was truncated at the old width; rebuild it so flex columns re-expand.
            _markupCache.Clear();
            foreach (var (key, item) in _items)
            {
                _markupCache[key] = _layoutRenderer.CreateRowMarkup(item);
            }
        }

        Panel.Width = AdaptiveLayout.PanelWidth(target);
    }
    
    // SetAll/Upsert/Remove/SetUiStatus are invoked from background (controller) threads. They
    // only mutate the model and flag the view dirty; the controller's invalidate() then wakes
    // the render loop, which renders on the main thread via RenderIfNeeded.
    public void SetAll(IReadOnlyList<T> items)
    {
        lock (_gate)
        {
            _items.Clear();
            _markupCache.Clear();
            foreach (var item in items)
            {
                _items[item.Key] = item;
                _markupCache[item.Key] = _layoutRenderer.CreateRowMarkup(item);
            }

            _sortDirty = true;
            _needsRender = true;
        }
    }

    public void Upsert(T item)
    {
        lock (_gate)
        {
            _items[item.Key] = item;
            // Only this row's markup is rebuilt. This is what turns the old O(N²) refresh
            // (every row rebuilt on every single-item update) into O(1) work per change.
            _markupCache[item.Key] = _layoutRenderer.CreateRowMarkup(item);
            _sortDirty = true;
            _needsRender = true;
        }
    }

    public void Remove(string key)
    {
        lock (_gate)
        {
            if (!_items.Remove(key))
            {
                return;
            }

            _markupCache.Remove(key);
            _sortDirty = true;
            _needsRender = true;
        }
    }

    public void SetUiStatus(UiStatusSnapshot uiStatusSnapshot)
    {
        _uiStatus = uiStatusSnapshot;
        lock (_gate)
        {
            _needsRender = true;
        }
    }

    public void SetHeader(string header)
    {
        lock (_gate)
        {
            _pendingHeader = header;
            _needsRender = true;
        }
    }

    public void SetEmptyStateMessage(string? message)
    {
        lock (_gate)
        {
            _dynamicEmptyState = message;
            _needsRender = true;
        }
    }

    public void HandleResize()
    {
        ApplyAdaptiveWidth();
        _paginator.UpdateMaxVisibleRows();
        RefreshView();
    }
    
    public void HandleInput(ConsoleKeyInfo keyInfo)
    {
        var scrolled = keyInfo.Key switch
        {
            ConsoleKey.PageUp   => _paginator.PageUp(),
            ConsoleKey.PageDown   => _paginator.PageDown(),
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
            RenderCurrentView();
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
        var rows = _visibleRows;
        if (rows.Count == 0)
        {
            return default;
        }

        // Defensive clamp: RebuildVisibleRows keeps SelectedIndex inside the window, but this is
        // also reachable between a background model change and the next rebuild, so never let a
        // stale index run off the end of the rows actually on screen. The sticky rows occupy the
        // leading slots, so the selected content row sits _stickyCount further down.
        var selectedIndex = Math.Clamp(_paginator.SelectedIndex + _stickyCount, 0, rows.Count - 1);
        var selectedItemKey = rows[selectedIndex].Key;
        lock (_gate)
        {
            _items.TryGetValue(selectedItemKey, out var item);
            return item;
        }
    }
    
    public string GetSelectedItemKey()
    {
        return GetSelectedItem()?.Key ?? "";
    }

    // Keys of the rows currently windowed for render, in display order. Reads the same
    // list the renderer consumes, so it faithfully reflects filtering, the bypass, and order.
    public IReadOnlyList<string> GetVisibleKeys() => _visibleRows.Select(r => r.Key).ToList();

    private void RefreshView()
    {
        lock (_gate)
        {
            _needsRender = false;
        }

        RebuildVisibleRows();
        RenderCurrentView();
    }

    // Called on the render (main) thread. Background updates only flag the view dirty; the
    // rebuild + Spectre table mutation happens here so the table is never written concurrently.
    public void RenderIfNeeded()
    {
        lock (_gate)
        {
            if (!_needsRender)
            {
                return;
            }
        }

        RefreshView();
    }

    public void RenderCurrentView()
    {
        ApplyPendingHeader();

        if (_visibleRows.Count == 0)
        {
            _renderer.RenderEmpty(GetEmptyStateMessage());
            return;
        }

        // Offset the highlight past the sticky (pinned) rows so it lands on the selected content row.
        var highlight = Math.Clamp(_paginator.SelectedIndex + _stickyCount, 0, _visibleRows.Count - 1);
        _renderer.Render(_visibleRows, highlight, _animationProvider.GetAnimations());
    }

    private void RebuildVisibleRows()
    {
        List<RowMarkup> rows;
        var stickyCount = 0;

        lock (_gate)
        {
            var sorted = GetSortedItemsLocked();

            if (string.IsNullOrWhiteSpace(_searchText))
            {
                (rows, stickyCount) = BuildUnfilteredRowsLocked(sorted);
            }
            else
            {
                var candidates = FilterCandidatesLocked(sorted);

                // No pinned rows under a filter (the bypass already floats active rows), so release
                // any reservation a previous unfiltered frame left behind.
                _paginator.SetReservedRows(0);

                // Reconcile the paginator to the current candidate count *before* slicing, so the
                // scroll offset and selection are clamped into range and the window we cut below can
                // never start past the end or point the selection outside _visibleRows.
                _paginator.UpdateTotalRows(candidates.Count);

                // Keep short result sets visible: if everything fits, ignore the scroll offset.
                var skip = candidates.Count < _paginator.MaxVisibleRows ? 0 : _paginator.VisibleStartIndex;

                rows = candidates
                    .Skip(skip)
                    .Take(_paginator.MaxVisibleRows)
                    // Bypassed rows are rendered on the fly (view-state cue, never highlighted);
                    // matching rows reuse their cached, once-built markup.
                    .Select(c => c.Bypassed ? _layoutRenderer.CreateBypassRowMarkup(c.Item) : _markupCache[c.Item.Key])
                    .ToList();
            }
        }

        _visibleRows = rows;
        _stickyCount = stickyCount;
    }

    // Builds the unfiltered view: the natural scroll window, with any row that has an active
    // operation AND is scrolled off the content window pinned (dimmed) to the top so a swap stays
    // watchable wherever you scroll. An on-screen active row is left in place (no pin, so never a
    // duplicate). The paginator reserves the pinned rows, shrinking the scroll window to fit them,
    // so the pins are always shown and the selected row is never pushed off. Returns (rows, pins).
    private (List<RowMarkup> Rows, int PinnedCount) BuildUnfilteredRowsLocked(IReadOnlyList<T> sorted)
    {
        _paginator.UpdateTotalRows(sorted.Count);
        var max = _paginator.MaxVisibleRows;
        var content = _paginator.ContentWindow;
        var start = sorted.Count <= content ? 0 : _paginator.VisibleStartIndex;
        var end = start + content;

        // Active rows outside the content window become pinned copies; on-screen ones are seen in
        // place. Leave at least one scrollable row.
        var pinnedItems = new List<T>();
        for (var i = 0; i < sorted.Count && pinnedItems.Count < max - 1; i++)
        {
            if ((i < start || i >= end) && sorted[i] is IOperationVisibility { HasActiveOperation: true })
            {
                pinnedItems.Add(sorted[i]);
            }
        }

        // Reserve space for the pins so the scroll window shrinks to fit them; takes effect on the
        // next rebuild (the row fill below caps at max, keeping this frame consistent meanwhile).
        _paginator.SetReservedRows(pinnedItems.Count);

        var rows = new List<RowMarkup>(max);
        foreach (var item in pinnedItems)
        {
            rows.Add(_layoutRenderer.CreateBypassRowMarkup(item));
        }

        for (var i = start; i < sorted.Count && rows.Count < max; i++)
        {
            rows.Add(_markupCache[sorted[i].Key]);
        }

        return (rows, pinnedItems.Count);
    }

    private readonly record struct Candidate(T Item, bool Bypassed);

    // Applies the search filter, then lets rows with an active operation (IOperationVisibility)
    // bypass a non-matching filter so an in-progress operation stays watchable. Bypassed rows
    // float to the top; matches keep their relative order below. Call while holding _gate, only
    // when a filter is active (the unfiltered view is built by BuildUnfilteredRowsLocked).
    private List<Candidate> FilterCandidatesLocked(IReadOnlyList<T> sorted)
    {
        var matches = new List<Candidate>();
        var bypassed = new List<Candidate>();
        foreach (var item in sorted)
        {
            if (_searchMatcher.TryMatch(item, _searchText))
            {
                matches.Add(new Candidate(item, false));
            }
            else if (item is IOperationVisibility { HasActiveOperation: true })
            {
                bypassed.Add(new Candidate(item, true));
            }
        }

        bypassed.AddRange(matches);
        return bypassed;
    }

    // Sorts by the active column (falling back to the model's natural IComparable order) and
    // caches the result until the model or the sort column changes. Call while holding _gate.
    private List<T> GetSortedItemsLocked()
    {
        if (!_sortDirty)
        {
            return _sortedCache;
        }

        var items = _items.Values.ToList();
        items.Sort();                                 // natural order (stable base for column sort)
        _sortedCache = _sorter.Sort(items).ToList();  // active column, or unchanged if none
        _sortDirty = false;
        return _sortedCache;
    }
    
    // Applied on the render thread only — the Panel header is Spectre state like the table.
    private void ApplyPendingHeader()
    {
        string? header;
        lock (_gate)
        {
            header = _pendingHeader;
            _pendingHeader = null;
        }

        if (header is not null)
        {
            Panel.Header(header, Justify.Center);
        }
    }

    private string? GetEmptyStateMessage()
    {
        int count;
        string? dynamicEmptyState;
        lock (_gate)
        {
            count = _items.Count;
            dynamicEmptyState = _dynamicEmptyState;
        }

        if (!string.IsNullOrWhiteSpace(_searchText) || count > 0)
        {
            return null;
        }

        return dynamicEmptyState ?? _emptyStateMessage?.Invoke(_uiStatus);
    }

    public bool TryGetNavigationRequest(out NavigationRequest? navigationRequest)
    {
        navigationRequest = null;
        if (_onEnterNavigation is null)
        {
            return false;
        }

        var selectedItem = GetSelectedItem();
        if (selectedItem is null)
        {
            return false;
        }

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
        if (selectedItem is null)
        {
            return false;
        }

        navigationRequest = _onActionNavigation(selectedItem);
        return navigationRequest is not null;
    }

    public Dictionary<TableIndex, ShortcutMap> GetShortcuts()
    {
        return _shortcuts.Describe(GetSelectedItem());
    }
    
    public UiStatusSnapshot GetUiStatusSnapshot() => _uiStatus;
    
    public bool IsActionValid(FunctionAction action)
    {
        var selectedItem = GetSelectedItem();
        return _shortcuts.IsActionValid(selectedItem, action);
    }
    
    public void SortViewBy(int keyInfoKey)
    {
        _sorter.Toggle(keyInfoKey);
        _renderer.ToggleSortingColumn(_sorter.CurrentColumn, _sorter.Desc);
        lock (_gate)
        {
            _sortDirty = true;
        }

        RefreshView();
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
