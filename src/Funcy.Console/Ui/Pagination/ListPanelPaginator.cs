namespace Funcy.Console.Ui.Pagination;

public class ListPanelPaginator
{
    public int SelectedIndex { get; private set; }
    public int VisibleStartIndex{ get; private set; }
    private int _amountOfRows;
    public int MaxVisibleRows { get; private set; }

    // Rows at the top reserved for pinned (non-scrollable) content. The scrollable list uses the
    // remaining rows, so all navigation/clamping happens against Window, not the raw height.
    private int _reservedRows;

    // Usable rows for the scrollable content (at least 1 so the view never collapses to nothing).
    public int ContentWindow => Math.Max(1, MaxVisibleRows - _reservedRows);

    // Reserve the top N rows for pinned content. Clamped so at least one scrollable row remains.
    public void SetReservedRows(int count)
    {
        _reservedRows = Math.Clamp(count, 0, Math.Max(0, MaxVisibleRows - 1));
    }

    // Window height source. Defaults to the real console; injectable so the paginator/view can
    // be exercised in headless tests (System.Console.WindowHeight throws without a console).
    private readonly Func<int> _windowHeight;

    public ListPanelPaginator(Func<int>? windowHeight = null)
    {
        _windowHeight = windowHeight ?? (() => System.Console.WindowHeight);

        // Ensure a valid window height before the first render. Previously SetItems primed
        // this via UpdateTotalRows; the targeted SetAll/Upsert path no longer does, so a
        // one-shot snapshot (e.g. the subscriptions panel) would otherwise Take(0) and show nothing.
        UpdateMaxVisibleRows();
    }

    public void UpdateTotalRows(int amountOfRows)
    {
        _amountOfRows = Math.Max(0, amountOfRows);
        UpdateMaxVisibleRows();

        if (SelectedIndex + VisibleStartIndex >= _amountOfRows)
        {
            SelectedIndex = Math.Min(SelectedIndex, _amountOfRows - VisibleStartIndex - 1);
            if (SelectedIndex < 0)
            {
                SelectedIndex = 0;
                VisibleStartIndex = 0;
            }
        }

        // The window can shrink faster than the selection moves — e.g. a terminal resize that
        // drops MaxVisibleRows, or the list changing under a scrolled window. When that happens the
        // clamp above (which only reacts to SelectedIndex + VisibleStartIndex crossing the total)
        // does not fire, leaving VisibleStartIndex past the last page and SelectedIndex past the
        // now-visible window. Both must be pulled back in, otherwise the view indexes _visibleRows
        // out of range (GetSelectedItem) and either throws or renders a blank frame.
        var maxStart = Math.Max(0, _amountOfRows - ContentWindow);
        if (VisibleStartIndex > maxStart)
        {
            VisibleStartIndex = maxStart;
        }

        if (SelectedIndex >= ContentWindow)
        {
            SelectedIndex = Math.Max(0, ContentWindow - 1);
        }
    }

    public bool MoveUp()
    {
        var isVisibleStartIndexChanged = false;
        
        SelectedIndex--;
                        
        if (SelectedIndex < 0 && VisibleStartIndex > 0)
        {
            isVisibleStartIndexChanged = true;
            VisibleStartIndex--;
            SelectedIndex = 0;
        }

        if (SelectedIndex < 0)
        {
            SelectedIndex = 0;
        }

        return isVisibleStartIndexChanged;
    }
    
    public bool PageUp()
    {
        var isVisibleStartIndexChanged = false;
        
        if (VisibleStartIndex > 0)
        {
            VisibleStartIndex = Math.Max(0, VisibleStartIndex - ContentWindow);
            isVisibleStartIndexChanged = true;
        }
        else
        {
            SelectedIndex = 0;
        }

        return isVisibleStartIndexChanged;
    }

    public bool MoveDown()
    {
        var isVisibleStartIndexChanged = false;
        SelectedIndex++;

        if (SelectedIndex >= ContentWindow && SelectedIndex + VisibleStartIndex < _amountOfRows)
        {
            isVisibleStartIndexChanged = true;
            VisibleStartIndex++;
            SelectedIndex = ContentWindow - 1;
        }

        if (SelectedIndex >= ContentWindow)
        {
            SelectedIndex = ContentWindow - 1;
        }

        if (SelectedIndex >= _amountOfRows)
        {
            SelectedIndex = _amountOfRows - 1;
        }

        return isVisibleStartIndexChanged;
    }

    public bool PageDown()
    {
        var isVisibleStartIndexChanged = false;

        if (VisibleStartIndex + ContentWindow >= _amountOfRows)
        {
            SelectedIndex = ContentWindow - 1;
        }
        else
        {
            VisibleStartIndex = Math.Min(_amountOfRows - ContentWindow, VisibleStartIndex + ContentWindow);
            isVisibleStartIndexChanged = true;
        }

        return isVisibleStartIndexChanged;
    }
    
    public void UpdateMaxVisibleRows()
    {
        // Floor at 0: a tiny terminal (windowHeight < 8) would otherwise make MaxVisibleRows
        // negative, which silently empties the view (Take(-n)) and corrupts skip/clamp math.
        MaxVisibleRows = Math.Max(0, _windowHeight() - 8);
    }
}