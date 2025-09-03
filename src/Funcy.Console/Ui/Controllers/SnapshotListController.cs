using Funcy.Console.Ui.Panels.Interfaces;
using Funcy.Core.Model;

namespace Funcy.Console.Ui.Controllers;

// C#
public sealed class SnapshotListController<T> : ListPanelControllerBase<T>
    where T : IComparable<T>, IHasKey
{
    private readonly Action? _invalidate;
    public SnapshotListController(IListPanelView<T> view, IEnumerable<T> initial, Action? invalidate = null)
        : base(view)
    {
        _invalidate = invalidate;
        
        Store.UpdateAll(initial);
        PushSnapshotToView();
        _invalidate?.Invoke();
    }
    
    private void OnUpdated(T updated)
    {
        Store.UpsertMany([updated]);
        PushSnapshotToView();
        _invalidate?.Invoke();
    }
}