using Funcy.Infrastructure.Model;

namespace Funcy.Console.Ui.Pagination;

public class FunctionAppPaginator()
{
    public int SelectedIndex { get; private set; }
    public int VisibleStartIndex{ get; private set; }
    private int _amountOfRows;
    public int MaxVisibleRows { get; private set; }

    public void OnDataUpdated(List<FunctionAppDetails> functionAppDetails)
    {
        _amountOfRows = functionAppDetails.Count;
        UpdateMaxVisibleRows();
        
        
        //Från gippy
        // if (_selectedIndex + _visibleStartIndex >= _amountOfRows)
        // {
        //     // Sätt markeringen på sista raden
        //     _selectedIndex = Math.Min(_selectedIndex, _amountOfRows - _visibleStartIndex - 1);
        //     if (_selectedIndex < 0)
        //     {
        //         _selectedIndex = 0;
        //         _visibleStartIndex = 0;
        //     }
        // }
        // EnsureSelectionVisible();
    }
    
    private void EnsureSelectionVisible()
    {
        // Om markeringen ligger ovanför viewport: skrolla upp
        if (SelectedIndex < 0)
            SelectedIndex = 0;

        if (SelectedIndex < 0 || SelectedIndex >= MaxVisibleRows) {
            // Flytta window så markeringen hamnar längst upp
            VisibleStartIndex = Math.Max(0, VisibleStartIndex + (SelectedIndex - 0));
            SelectedIndex = 0;
        }
        // Om markeringen ligger under viewport: skrolla ner
        else if (SelectedIndex >= MaxVisibleRows)
        {
            VisibleStartIndex += (SelectedIndex - (MaxVisibleRows - 1));
            SelectedIndex = MaxVisibleRows - 1;
        }
    
        // Slutligen: om VisibleStartIndex är utanför range
        VisibleStartIndex = Math.Clamp(VisibleStartIndex, 0, Math.Max(0, _amountOfRows - MaxVisibleRows));
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
        MaxVisibleRows = Math.Min(System.Console.WindowHeight - 8, _amountOfRows);
    }
}