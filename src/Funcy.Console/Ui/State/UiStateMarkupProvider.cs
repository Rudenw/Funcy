using Funcy.Console.Handlers;
using Spectre.Console;

namespace Funcy.Console.Ui.State;

public class UiStateMarkupProvider(IAnimationProvider animationProvider)
{
    public Markup CreateMarkupFromUiStatusState(UiStatusSnapshot state)
    {
        var animations = animationProvider.GetAnimation("TopPanel");
        var animationFrame = animations?.AnimationFrame ?? string.Empty;
        if (state.IsInventoryValidating)
        {
            return UiStyles.CreateStatusText($"Validating all function apps {animationFrame}");
        }

        if (state.IsDetailsRefreshing)
        {
            return UiStyles.CreateStatusText($"Refreshing function app details {state.DetailsInFlight}/{state.TotalDetails}");
        }
        
        var lastUpdated = new DateTime(state.LastInventoryRefreshUtcTicks, DateTimeKind.Utc);
        return new Markup($"Last Updated {lastUpdated:HH:mm}");
    }
}