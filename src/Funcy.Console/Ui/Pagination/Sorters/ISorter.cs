using Funcy.Core.Model;

namespace Funcy.Console.Ui.Pagination.Sorters;

public interface ISorter<T>
{
    void Toggle(int columnIndex);
    // Forces a specific ascending/descending sort on a column (no 'off' state), for panels that
    // expose a plain two-way order toggle rather than the three-state column cycle.
    void SetOrder(int columnIndex, bool descending);
    IReadOnlyList<T> Sort(IReadOnlyList<T> snapshot);
    int? CurrentColumn { get; }
    bool Desc { get; }
}