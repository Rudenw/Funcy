using Funcy.Console.Ui.Panels.GenericTestPanel;
using Funcy.Core.Model;

namespace Funcy.Console.Ui.Controllers;

// C#
public sealed class SnapshotListController<T> : ListPanelControllerBase<T>
    where T : IComparable<T>, IHasKey
{
    public SnapshotListController(IListPanelView<T> view, IEnumerable<T> initial)
        : base(view)
    {
        Store.UpdateAll(initial);
        PushSnapshotToView();
    }
    
    public void Replace(IEnumerable<T> items)
    {
        Store.UpdateAll(items);
        PushSnapshotToView();
    }
}