using Funcy.Console.Handlers;
using Spectre.Console;

namespace Funcy.Console.Ui.State;

public class UiStateMarkupProvider()
{
    public Markup CreateMarkupFromUiStatusState(UiStatusSnapshot state)
    {
        if (state.IsInventoryValidating)
        {
            return UiStyles.CreateStatusText($"Validating all function apps {state.DetailsInFlight}");
        }

        if (state.IsDetailsRefreshing)
        {
            return UiStyles.CreateStatusText($"Refreshing function app details {state.DetailsInFlight}/{state.TotalDetails}");
        }
        
        var lastUpdated = new DateTime(state.LastInventoryRefreshUtcTicks, DateTimeKind.Utc);
        return new Markup($"Last Updated {lastUpdated:HH:mm}");
    }
}