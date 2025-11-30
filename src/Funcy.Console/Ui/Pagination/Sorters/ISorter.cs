using Funcy.Core.Model;

namespace Funcy.Console.Ui.Pagination.Sorters;

public interface ISorter<T>
{
    void Toggle(int columnIndex);
    IReadOnlyList<T> Sort(IReadOnlyList<T> snapshot);
    int? CurrentColumn { get; }
    bool Desc { get; }
}