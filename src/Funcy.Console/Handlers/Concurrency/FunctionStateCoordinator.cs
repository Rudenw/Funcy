using System.Collections.Concurrent;
using System.Threading.Channels;
using Funcy.Console.Handlers.Models;
using Funcy.Core.Model;

namespace Funcy.Console.Handlers.Concurrency;

public class FunctionStateCoordinator
{
    private readonly Channel<FunctionAppDetails> _updateChannel = Channel.CreateUnbounded<FunctionAppDetails>();
    private readonly Channel<FunctionAppDetails> _removeChannel = Channel.CreateUnbounded<FunctionAppDetails>();

    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, CachedFunctionAppModel>> _cache = new();

    private readonly SemaphoreSlim _uiUpdateLock = new(1, 1);
    private string? _currentSubscriptionName;
    public event Action<List<FunctionAppDetails>>? OnCacheInit;
    public event Action<FunctionAppDetails>? OnFunctionAppUpdated;
    public event Action<FunctionAppDetails>? OnFunctionAppRemoved;
    public event Action<string, List<FunctionDetails>>? OnFunctionListUpdated;

    public FunctionStateCoordinator(AppContext appContext)
    {
        _ = ProcessUpdatesAsync();
        _ = ProcessRemovalsAsync();
    }
    
    public void SetSubscription(string subscriptionId)
    {
        _currentSubscriptionName = subscriptionId;
        _cache.GetOrAdd(subscriptionId, _ => new ConcurrentDictionary<string, CachedFunctionAppModel>());
    }
    
    private ConcurrentDictionary<string, CachedFunctionAppModel> GetCurrentCache()
    {
        return _cache.GetOrAdd(_currentSubscriptionName!, _ => new ConcurrentDictionary<string, CachedFunctionAppModel>());
    }
    
    public void InitCache(List<FunctionAppDetails> functionsFromDatabase)
    {
        var subCache = GetCurrentCache();
        subCache.Clear();
        foreach (var functionApp in functionsFromDatabase)
        {
            subCache.TryAdd(functionApp.Name, new CachedFunctionAppModel(functionApp, functionApp.LastUpdated));
        }
        OnCacheInit?.Invoke(functionsFromDatabase);
    }

    public List<FunctionAppDetails> GetCachedFunctionAppDetails()
    {
        return GetCurrentCache().Values.Select(f => f.FunctionAppDetails).ToList();
    }

    public FunctionAppDetails? TryGet(string key)
    {
        GetCurrentCache().TryGetValue(key, out var app);
        return app?.FunctionAppDetails;
    }

    public async Task PublishUpdateAsync(FunctionAppDetails details)
    {
        await _updateChannel.Writer.WriteAsync(details);
    }
    
    private async Task PublishRemoveAsync(FunctionAppDetails removedApp)
    {
        await _removeChannel.Writer.WriteAsync(removedApp);
    }
    
    public async Task RemoveFunctions(List<string> existingFunctionAppNames)
    {
        var subCache = GetCurrentCache();
        var removedFunctions = subCache.Keys.Except(existingFunctionAppNames).Select(functionApp => subCache[functionApp]).ToList();
        foreach (var removedFunction in removedFunctions)
        {
            await PublishRemoveAsync(removedFunction.FunctionAppDetails);
        }
    }

    private async Task ProcessUpdatesAsync()
    {
        await foreach (var update in _updateChannel.Reader.ReadAllAsync())
        {
            if (update.Subscription != _currentSubscriptionName)
            {
                continue;
            }
            
            var subCache = GetCurrentCache();
            var status = update.Status;
            subCache[update.Name] =
                new CachedFunctionAppModel(update, update.LastUpdated)
                {
                    FunctionAppDetails =
                    {
                        Status = status
                    }
                };
            await _uiUpdateLock.WaitAsync();
            try
            {
                OnFunctionAppUpdated?.Invoke(update);
                OnFunctionListUpdated?.Invoke(update.Key, update.Functions);
            }
            finally
            {
                _uiUpdateLock.Release();
            }
        }
    }
    
    private async Task ProcessRemovalsAsync()
    {
        await foreach (var removedApp in _removeChannel.Reader.ReadAllAsync())
        {
            _cache.TryRemove(removedApp.Name, out _);

            await _uiUpdateLock.WaitAsync();
            try
            {
                OnFunctionAppRemoved?.Invoke(removedApp);
            }
            finally
            {
                _uiUpdateLock.Release();
            }
        }
    }
}