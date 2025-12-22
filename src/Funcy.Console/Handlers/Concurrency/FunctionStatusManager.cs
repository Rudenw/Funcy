using Funcy.Core.Model;

namespace Funcy.Console.Handlers.Concurrency;

public class FunctionStatusManager(
    FunctionStateCoordinator functionStateCoordinator,
    AnimationHandler animationHandler)
{
    public async Task BeginOperation(FunctionAppDetails details, FunctionAction action)
    {
        details.Status.Status = StatusType.InProgress;
        details.Status.Action = action;
        details.LastUpdated = DateTime.UtcNow;

        animationHandler.AddAppDetails(details.Key);
        await functionStateCoordinator.PublishUpdateAsync(details);
    }

    public async Task CompleteOperation(FunctionAppDetails details, FunctionAction action, bool success)
    {
        details.Status.Status = success
            ? (action == FunctionAction.Swap ? StatusType.Swapped : StatusType.Success)
            : StatusType.Error;
        details.Status.Action = null;
        details.LastUpdated = DateTime.UtcNow;

        animationHandler.RemoveAppDetails(details.Key);
        await functionStateCoordinator.PublishUpdateAsync(details);

        _ = ResetToIdleAfterDelay(details);
    }

    private async Task ResetToIdleAfterDelay(FunctionAppDetails details)
    {
        var timeToLive = details.Status.GetTimeToLive();
        if (timeToLive <= 0) return;

        await Task.Delay(TimeSpan.FromSeconds(timeToLive));
        details.Status.Status = StatusType.Idle;
        await functionStateCoordinator.PublishUpdateAsync(details);
    }
}