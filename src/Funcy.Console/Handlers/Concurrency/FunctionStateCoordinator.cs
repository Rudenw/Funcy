using System.Collections.Concurrent;
using System.Threading.Channels;
using Funcy.Console.Handlers.Models;
using Funcy.Core.Model;

namespace Funcy.Console.Concurrency;

public class FunctionStateCoordinator
{
    private readonly Channel<FunctionAppUpdate> _updateChannel = Channel.CreateUnbounded<FunctionAppUpdate>();
    private readonly Channel<FunctionAppUpdate> _removeChannel = Channel.CreateUnbounded<FunctionAppUpdate>();
    
    private readonly ConcurrentDictionary<string, FunctionAppDetails> _cache = new();
    
    private readonly SemaphoreSlim _uiUpdateLock = new(1, 1);
    public event Action<FunctionAppDetails>? OnFunctionAppUpdated;
    public event Action<FunctionAppDetails>? OnFunctionAppRemoved;

    public FunctionStateCoordinator()
    {
        _ = ProcessUpdatesAsync();
        _ = ProcessRemovalsAsync();
    }
    
    public void InitCache(List<FunctionAppDetails> functionsFromDatabase)
    {
        foreach (var functionApp in functionsFromDatabase)
        {
            _cache.TryAdd(functionApp.Name, functionApp);
        }
    }

    public List<FunctionAppDetails> GetInitialLoad()
    {
        return _cache.Values.ToList();
    }
    
    public FunctionAppDetails? TryGet(string key)
    {
        _cache.TryGetValue(key, out var app);
        return app;
    }

    public async Task PublishUpdateAsync(FunctionAppUpdate details)
    {
        await _updateChannel.Writer.WriteAsync(details);
    }

    private async Task PublishRemoveAsync(FunctionAppUpdate removedApp)
    {
        await _removeChannel.Writer.WriteAsync(removedApp);
    }

    public async Task<List<FunctionAppDetails>> RemoveFunctions(List<string> existingFunctionAppNames)
    {
        var removedFunctions = _cache.Keys.Except(existingFunctionAppNames).Select(functionApp => _cache[functionApp]).ToList();
        foreach (var removedFunction in removedFunctions)
        {
            await PublishRemoveAsync(new FunctionAppUpdate
            {
                FunctionAppDetails = removedFunction,
                Source = UpdateSource.Database
            });
        }

        return removedFunctions;
    }

    private async Task ProcessUpdatesAsync()
    {
        await foreach (var update in _updateChannel.Reader.ReadAllAsync())
        {
            var isSwapping = _cache[update.FunctionAppDetails.Name].Status.IsSwapping;
            if (isSwapping && update.Source == UpdateSource.Database)
            {
                continue;
            }
            
            _cache[update.FunctionAppDetails.Name] = update.FunctionAppDetails;
            await _uiUpdateLock.WaitAsync();
            try
            {
                OnFunctionAppUpdated?.Invoke(update.FunctionAppDetails);
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
            _cache.TryRemove(removedApp.FunctionAppDetails.Name, out _);

            await _uiUpdateLock.WaitAsync();
            try
            {
                OnFunctionAppRemoved?.Invoke(removedApp.FunctionAppDetails);
            }
            finally
            {
                _uiUpdateLock.Release();
            }
        }
    }
}