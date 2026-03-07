using Funcy.Console.Handlers.Concurrency;
using Funcy.Core.Model;
using Funcy.Infrastructure.Azure;
using Microsoft.Extensions.Logging;

namespace Funcy.Console.Handlers;

public class SubscriptionProbeHandler(
    IAzureResourceService azureResourceService,
    FunctionStateCoordinator coordinator,
    AppContext appContext,
    ILogger<SubscriptionProbeHandler> logger)
{
    public async Task ProbeAllSubscriptionsAsync(CancellationToken token)
    {
        var subscriptions = appContext.GetSnapshot()
            .Where(s => !s.Current)
            .ToList();

        using var throttler = new SemaphoreSlim(3, 3);
        var tasks = subscriptions.Select(sub => ProbeSubscriptionAsync(sub, throttler, token));
        await Task.WhenAll(tasks);
    }

    private async Task ProbeSubscriptionAsync(SubscriptionDetails sub, SemaphoreSlim throttler, CancellationToken token)
    {
        await throttler.WaitAsync(token);
        try
        {
            var hasApps = await azureResourceService.HasAnyFunctionAppsAsync(sub.Id);
            if (!hasApps)
            {
                coordinator.MarkSubscriptionAsEmpty(sub.Id);
                logger.LogInformation("Subscription {SubscriptionId} has no function apps", sub.Id);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "Failed to probe subscription {SubscriptionId}", sub.Id);
        }
        finally
        {
            throttler.Release();
        }
    }
}
