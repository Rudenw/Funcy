using Funcy.Core.Model;
using Funcy.Infrastructure.Azure;

namespace Funcy.Console;

public class AppContext(AzureSubscriptionService azureSubscriptionService)
{
    private SubscriptionDetails? _currentSubscription;
    public SubscriptionDetails CurrentSubscription =>
        _currentSubscription ?? throw new InvalidOperationException("AppContext not initialized");
    private Dictionary<string, SubscriptionDetails> CachedSubscriptions { get; set; } = [];
    public event Action<SubscriptionDetails>? OnSubscriptionChange;

    public async Task InitializeAppContext()
    {
        var subscriptions = await azureSubscriptionService.GetSubscriptions();
        SetCachedSubscriptions(subscriptions);
        
        _currentSubscription ??=
            subscriptions.FirstOrDefault(s => s.Current)
            ?? throw new InvalidOperationException("No current subscription resolved");
    }

    private void SetCachedSubscriptions(List<SubscriptionDetails> subscriptions)
    {
        var newCache = new Dictionary<string, SubscriptionDetails>();
        foreach (var subscription in subscriptions)
        {
            subscription.Current = _currentSubscription is not null ? subscription.Key == CurrentSubscription.Key : subscription.Current;
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
        
        if (CurrentSubscription.Key == subscriptionKey) return;
        
        _currentSubscription = subscription;
        CachedSubscriptions.Values.ToList().ForEach(s => s.Current = s.Key == subscriptionKey);
        
        OnSubscriptionChange?.Invoke(_currentSubscription);
    }
}