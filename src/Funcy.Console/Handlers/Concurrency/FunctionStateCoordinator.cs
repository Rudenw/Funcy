using System.Collections.Concurrent;
using System.Threading.Channels;
using Funcy.Console.Handlers.Models;
using Funcy.Core.Model;

namespace Funcy.Console.Handlers.Concurrency;

public class FunctionStateCoordinator
{
    private readonly Channel<FunctionAppUpdate> _updateChannel = Channel.CreateUnbounded<FunctionAppUpdate>();

    private readonly ConcurrentDictionary<string, CachedFunctionAppModel> _cache = new();

    private readonly SemaphoreSlim _uiUpdateLock = new(1, 1);
    public event Action<FunctionAppDetails>? OnFunctionAppUpdated;
    public event Action<string, List<FunctionDetails>>? OnFunctionListUpdated;

    public FunctionStateCoordinator()
    {
        _ = ProcessUpdatesAsync();
    }

    public List<FunctionAppDetails> GetInitialLoad()
    {
        return _cache.Values.Select(f => f.FunctionAppDetails).ToList();
    }

    public FunctionAppDetails? TryGet(string key)
    {
        _cache.TryGetValue(key, out var app);
        return app?.FunctionAppDetails;
    }

    public async Task PublishUpdateAsync(FunctionAppUpdate details)
    {
        await _updateChannel.Writer.WriteAsync(details);
    }

    private async Task ProcessUpdatesAsync()
    {
        await foreach (var update in _updateChannel.Reader.ReadAllAsync())
        {
            var status = update.FunctionAppDetails.Status;
            _cache[update.FunctionAppDetails.Name] =
                new CachedFunctionAppModel(update.FunctionAppDetails, update.FunctionAppDetails.LastUpdated)
                {
                    FunctionAppDetails =
                    {
                        Status = status
                    }
                };
            await _uiUpdateLock.WaitAsync();
            try
            {
                OnFunctionAppUpdated?.Invoke(update.FunctionAppDetails);
                OnFunctionListUpdated?.Invoke(update.FunctionAppDetails.Key, update.FunctionAppDetails.Functions);
            }
            finally
            {
                _uiUpdateLock.Release();
            }
        }
    }
}