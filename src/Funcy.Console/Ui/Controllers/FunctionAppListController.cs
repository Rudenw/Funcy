using Funcy.Console.Handlers.Concurrency;
using Funcy.Console.Ui.Panels.Interfaces;
using Funcy.Core.Model;

namespace Funcy.Console.Ui.Controllers;

// C#
public sealed class FunctionAppListController : ListPanelControllerBase<FunctionAppDetails>
{
    private readonly FunctionStateCoordinator _coordinator;
    private readonly Action? _invalidate;

    public FunctionAppListController(IListPanelView<FunctionAppDetails> view,
        IEnumerable<FunctionAppDetails> initial,
        FunctionStateCoordinator coordinator,
        Action? invalidate = null)
        : base(view)
    {
        _coordinator = coordinator;
        _invalidate = invalidate;
        
        Store.UpdateAll(initial);
        PushSnapshotToView();
        _invalidate?.Invoke();
        
        _coordinator.OnFunctionAppUpdated += OnUpdated;
    }

    private void OnUpdated(FunctionAppDetails updated)
    {
        Store.UpsertMany([updated]);
        PushSnapshotToView();
        _invalidate?.Invoke();
    }

    public override void Dispose()
    {
        _coordinator.OnFunctionAppUpdated -= OnUpdated;
        base.Dispose();
    }
}