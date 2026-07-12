namespace Funcy.Console.Ui.Panels.Interfaces;

// A panel that exposes a plain two-way sort order (ascending/descending) on a single column,
// rather than the three-state digit-key column cycle. Used by the logs panel's oldest/newest
// toggle.
public interface IOrderTogglePanel
{
    void SetSortOrder(int columnIndex, bool descending);
}
