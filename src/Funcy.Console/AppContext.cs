using System.Collections.Concurrent;
using Funcy.Core.Model;
using Funcy.Infrastructure.Azure;

namespace Funcy.Console;

public class AppContext(AzureSubscriptionService azureSubscriptionService)
{
    public SubscriptionDetails? CurrentSubscription { get; private set; }
    private Dictionary<string, SubscriptionDetails> CachedSubscriptions { get; set; } = [];
    public event Action<SubscriptionDetails>? OnSubscriptionChange;

    public async Task InitializeAppContext()
    {
        var subscriptions = await azureSubscriptionService.GetSubscriptions();
        SetCachedSubscriptions(subscriptions);
    }

    private void SetCachedSubscriptions(List<SubscriptionDetails> subscriptions)
    {
        var newCache = new Dictionary<string, SubscriptionDetails>();
        foreach (var subscription in subscriptions)
        {
            if (subscription.Current)
            {
                CurrentSubscription = subscription;
            }
            newCache.TryAdd(subscription.Key, subscription);
        }

        CachedSubscriptions = newCache;
    }
    
    public IReadOnlyList<SubscriptionDetails> GetSnapshot() => CachedSubscriptions.Values.ToList();
    
    public void ChangeSubscription(string subscriptionKey)
    {
        if (!CachedSubscriptions.TryGetValue(subscriptionKey, out var subscription))
        {
            throw new KeyNotFoundException($"Subscription '{subscriptionKey}' not found");
        }
        
        if (CurrentSubscription?.Key == subscriptionKey) return;
        
        CurrentSubscription = subscription;
        
        OnSubscriptionChange?.Invoke(CurrentSubscription);
    }
}