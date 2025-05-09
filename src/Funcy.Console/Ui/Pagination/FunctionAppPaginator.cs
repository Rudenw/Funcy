using Funcy.Infrastructure.Model;

namespace Funcy.Console.Ui.Pagination;

public class FunctionAppPaginator
{
    private int _selectedIndex;
    private int _visibleStartIndex;
    private int _amountOfRows;
    private int _maxVisibleRows = 5;

    public void OnDataChanged(List<FunctionAppDetails> functionAppDetails)
    {
        _amountOfRows = functionAppDetails.Count;
        UpdateMaxVisibleRows();
    }

    public PaginatorResult MoveUp()
    {
        var isVisibleStartIndexChanged = false;
        
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

        return new PaginatorResult(_visibleStartIndex, _selectedIndex, isVisibleStartIndexChanged);
    }

    public PaginatorResult MoveDown()
    {
        var isVisibleStartIndexChanged = false;
        _selectedIndex++;

        if (_selectedIndex >= _maxVisibleRows && _selectedIndex + _visibleStartIndex < _amountOfRows)
        {
            isVisibleStartIndexChanged = true;
            _visibleStartIndex++;
            _selectedIndex = _maxVisibleRows - 1;
        }

        if (_selectedIndex >= _maxVisibleRows)
        {
            _selectedIndex = _maxVisibleRows - 1;
        }

        return new PaginatorResult(_visibleStartIndex, _selectedIndex, isVisibleStartIndexChanged);
    }
    
    private void UpdateMaxVisibleRows()
    {
        _maxVisibleRows = Math.Min(System.Console.WindowHeight - 8, _amountOfRows);
    }
}