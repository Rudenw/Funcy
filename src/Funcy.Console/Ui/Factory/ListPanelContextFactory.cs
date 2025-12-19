using Funcy.Console.Handlers;
using Funcy.Console.Handlers.Concurrency;
using Funcy.Console.Ui.Contexts;
using Funcy.Console.Ui.Controllers;
using Funcy.Console.Ui.Navigation;
using Funcy.Console.Ui.Panels;
using Funcy.Console.Ui.Panels.Interfaces;
using Funcy.Core.Model;

namespace Funcy.Console.Ui.Factory;

public sealed class ListPanelContextFactory(
    FunctionStateCoordinator coordinator,
    ListPanelFactory listPanelFactory,
    IUiStatusState uiStatusState)
{
    public ListPanelContext CreateRoot(IReadOnlyList<FunctionAppDetails> apps, Action invalidate)
    {
        var panel = listPanelFactory.CreateFunctionAppPanel(apps);
        var controller = new FunctionAppListController(
            (IListPanelView<FunctionAppDetails>)panel,
            apps,
            coordinator,
            uiStatusState,
            invalidate: invalidate);

        return new ListPanelContext
        {
            View = panel,
            Controller = controller
        };
    }

    public ListPanelContext CreateFromNavigation(NavigationRequest request, Action invalidate)
    {
        var panel = listPanelFactory.Create(request);
        var app = coordinator.TryGet(request.Key)
                  ?? throw new InvalidOperationException($"App not found: {request.Key}");

        switch (request.Target)
        {
            case PanelTarget.Functions:
            {
                var view = (IListPanelView<FunctionDetails>)panel;
                var controller = new FunctionListController(view, app.Key, app.Functions, coordinator, invalidate);
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