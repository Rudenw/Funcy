using Funcy.Console.Handlers.Concurrency;
using Funcy.Console.Ui.Panels.Interfaces;
using Funcy.Core.Model;

namespace Funcy.Console.Ui.Controllers;

// C#
public sealed class FunctionListController : ListPanelControllerBase<FunctionDetails>
{
    private readonly FunctionStateCoordinator _coordinator;
    private readonly Action? _invalidate;
    private readonly string FunctionAppKey;

    public FunctionListController(IListPanelView<FunctionDetails> view,
        string appKey,
        IEnumerable<FunctionDetails> initial,
        FunctionStateCoordinator coordinator,
        Action? invalidate = null)
        : base(view)
    {
        _coordinator = coordinator;
        _invalidate = invalidate;
        FunctionAppKey = appKey;
        
        Store.UpdateAll(initial);
        PushSnapshotToView();
        _invalidate?.Invoke();
        
        _coordinator.OnFunctionListUpdated += OnListUpdated;
    }

    private void OnListUpdated(string functionAppKey, List<FunctionDetails> updated)
    {
        if (string.Equals(FunctionAppKey, functionAppKey))
        {
            Store.UpsertMany(updated);
            PushSnapshotToView();
            _invalidate?.Invoke();
        }

    }

    public override void Dispose()
    {
        _coordinator.OnFunctionListUpdated -= OnListUpdated;
        base.Dispose();
    }
}