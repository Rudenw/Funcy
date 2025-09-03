using Funcy.Console.Handlers.Concurrency;
using Funcy.Console.Ui.Contexts;
using Funcy.Console.Ui.Controllers;
using Funcy.Console.Ui.Navigation;
using Funcy.Console.Ui.Panels;
using Funcy.Console.Ui.Panels.Interfaces;
using Funcy.Core.Model;

namespace Funcy.Console.Ui.Factory;

public sealed class ListPanelContextFactory(FunctionStateCoordinator coordinator,
    Action invalidate)
{
    private readonly ListPanelFactory _listPanelFactory = new(coordinator.TryGet);
    
    public ListPanelContext CreateRoot(IReadOnlyList<FunctionAppDetails> apps)
    {
        var panel = _listPanelFactory.CreateFunctionAppPanel(apps);
        var controller = new FunctionAppListController(
            (IListPanelView<FunctionAppDetails>)panel,
            apps,
            coordinator,
            invalidate: invalidate);

        return new ListPanelContext
        {
            View = panel,
            Controller = controller
        };
    }

    public ListPanelContext CreateFromNavigation(NavigationRequest request)
    {
        var panel = _listPanelFactory.Create(request);
        var app = coordinator.TryGet(request.Key)
                  ?? throw new InvalidOperationException($"App not found: {request.Key}");

        switch (request.Target)
        {
            case PanelTarget.Functions:
            {
                var view = (IListPanelView<FunctionDetails>)panel;
                var controller = new SnapshotListController<FunctionDetails>(view, app.Functions, invalidate);
                return new ListPanelContext
                {
                    View = panel,
                    Controller = controller
                };
            }
            case PanelTarget.Slots:
            {
                var view = (IListPanelView<FunctionAppSlotDetails>)panel;
                var controller = new SnapshotListController<FunctionAppSlotDetails>(view, app.Slots, invalidate);
                return new ListPanelContext
                {
                    View = panel,
                    Controller = controller
                };
            }
            default:
                throw new NotSupportedException($"Unknown target: {request.Target}");
        }
    }
}