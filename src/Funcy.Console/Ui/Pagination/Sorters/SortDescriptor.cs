namespace Funcy.Console.Ui.Pagination.Sorters;

public class SortDescriptor<T>
{
    public int ColumnIndex { get; }
    public Func<T, object?> Selector { get; }

    public SortDescriptor(int columnIndex, Func<T, object?> selector)
    {
        ColumnIndex = columnIndex;
        Selector = selector;
    }
}