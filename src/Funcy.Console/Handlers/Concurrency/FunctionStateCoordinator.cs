using System.Collections.Concurrent;
using System.Threading.Channels;
using Funcy.Core.Model;

namespace Funcy.Console.Concurrency;

public class FunctionStateCoordinator
{
    private readonly Channel<FunctionAppDetails> _updateChannel = Channel.CreateUnbounded<FunctionAppDetails>();
    private readonly Channel<FunctionAppDetails> _removeChannel = Channel.CreateUnbounded<FunctionAppDetails>();
    
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

    public async Task PublishUpdateAsync(FunctionAppDetails details)
    {
        await _updateChannel.Writer.WriteAsync(details);
    }

    private async Task PublishRemoveAsync(FunctionAppDetails removedApp)
    {
        await _removeChannel.Writer.WriteAsync(removedApp);
    }

    public async Task<List<FunctionAppDetails>> RemoveFunctions(List<string> existingFunctionAppNames)
    {
        var removedFunctions = _cache.Keys.Except(existingFunctionAppNames).Select(functionApp => _cache[functionApp]).ToList();
        foreach (var removedFunction in removedFunctions)
        {
            await PublishRemoveAsync(removedFunction);
        }

        return removedFunctions;
    }

    private async Task ProcessUpdatesAsync()
    {
        await foreach (var update in _updateChannel.Reader.ReadAllAsync())
        {
            if (_cache.TryGetValue(update.Name, out var old) && update.Equals(old)) continue;
            
            _cache[update.Name] = update;
            await _uiUpdateLock.WaitAsync();
            try
            {
                OnFunctionAppUpdated?.Invoke(update);
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