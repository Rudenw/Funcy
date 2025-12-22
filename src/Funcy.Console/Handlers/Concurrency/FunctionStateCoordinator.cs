using System.Collections.Concurrent;
using System.Threading.Channels;
using Funcy.Console.Handlers.Models;
using Funcy.Core.Model;

namespace Funcy.Console.Handlers.Concurrency;

public class FunctionStateCoordinator
{
    private readonly Channel<FunctionAppDetails> _updateChannel = Channel.CreateUnbounded<FunctionAppDetails>();
    private readonly Channel<FunctionAppDetails> _removeChannel = Channel.CreateUnbounded<FunctionAppDetails>();

    private readonly ConcurrentDictionary<string, CachedFunctionAppModel> _cache = new();

    private readonly SemaphoreSlim _uiUpdateLock = new(1, 1);
    public event Action<FunctionAppDetails>? OnFunctionAppUpdated;
    public event Action<FunctionAppDetails>? OnFunctionAppRemoved;
    public event Action<string, List<FunctionDetails>>? OnFunctionListUpdated;

    public FunctionStateCoordinator()
    {
        _ = ProcessUpdatesAsync();
        _ = ProcessRemovalsAsync();
    }
    
    public void InitCache(List<FunctionAppDetails> functionsFromDatabase)
    {
        foreach (var functionApp in functionsFromDatabase)
        {
            _cache.TryAdd(functionApp.Name, new CachedFunctionAppModel(functionApp, functionApp.LastUpdated));
        }
    }

    public List<FunctionAppDetails> GetCachedFunctionAppDetails()
    {
        return _cache.Values.Select(f => f.FunctionAppDetails).ToList();
    }

    public FunctionAppDetails? TryGet(string key)
    {
        _cache.TryGetValue(key, out var app);
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
        var removedFunctions = _cache.Keys.Except(existingFunctionAppNames).Select(functionApp => _cache[functionApp]).ToList();
        foreach (var removedFunction in removedFunctions)
        {
            await PublishRemoveAsync(removedFunction.FunctionAppDetails);
        }
    }

    private async Task ProcessUpdatesAsync()
    {
        await foreach (var update in _updateChannel.Reader.ReadAllAsync())
        {
            var status = update.Status;
            _cache[update.Name] =
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