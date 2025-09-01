using Funcy.Console.Ui.Panels;
using Funcy.Console.Ui.Panels.Interfaces;
using Funcy.Core.Model;

namespace Funcy.Console.Ui.Controllers;

// C#
public interface IListController : IDisposable { }

public abstract class ListPanelControllerBase<T>(IListPanelView<T> view) : IListController
    where T : IComparable<T>, IHasKey
{
    protected readonly ListPanelDataStore<T> Store = new();
    protected readonly IListPanelView<T> View = view;

    protected void PushSnapshotToView()
    {
        var snapshot = Store.Snapshot();
        View.SetItems(snapshot);
    }

    public virtual void Dispose() { /* unhook events etc */ }
}