namespace Funcy.Console.Ui.Pagination;

public class FunctionAppPaginator()
{
    public int SelectedIndex { get; private set; }
    public int VisibleStartIndex{ get; private set; }
    private int _amountOfRows;
    public int MaxVisibleRows { get; private set; }

    public void UpdateTotalRows(int amountOfRows)
    {
        _amountOfRows = amountOfRows;
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

    public bool MoveDown()
    {
        var isVisibleStartIndexChanged = false;
        SelectedIndex++;

        if (SelectedIndex >= MaxVisibleRows && SelectedIndex + VisibleStartIndex < _amountOfRows)
        {
            isVisibleStartIndexChanged = true;
            VisibleStartIndex++;
            SelectedIndex = MaxVisibleRows - 1;
        }

        if (SelectedIndex >= MaxVisibleRows)
        {
            SelectedIndex = MaxVisibleRows - 1;
        }

        return isVisibleStartIndexChanged;
    }
    
    public void UpdateMaxVisibleRows()
    {
        MaxVisibleRows = System.Console.WindowHeight - 7;
    }
}