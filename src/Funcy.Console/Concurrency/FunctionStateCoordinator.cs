using System.Collections.Concurrent;
using System.Threading.Channels;
using Funcy.Core.Model;

namespace Funcy.Console.Concurrency;

public class FunctionStateCoordinator
{
    private readonly Channel<FunctionAppDetails> _updateChannel = Channel.CreateUnbounded<FunctionAppDetails>();
    private readonly ConcurrentDictionary<string, FunctionAppDetails> _functionApps = new();
    private readonly SemaphoreSlim _uiUpdateLock = new(1, 1);

    public FunctionStateCoordinator()
    {
        _ = ProcessUpdatesAsync();
    }

    public async Task PublishUpdateAsync(FunctionAppDetails details)
    {
        await _updateChannel.Writer.WriteAsync(details);
    }

    private async Task ProcessUpdatesAsync()
    {
        await foreach (var update in _updateChannel.Reader.ReadAllAsync())
        {
            _functionApps.AddOrUpdate(update.Name, update, (_, old) => update);
            
            await _uiUpdateLock.WaitAsync();
            try
            {
                // TODO: pusha till din lista i UI eller kalla en callback
            }
            finally
            {
                _uiUpdateLock.Release();
            }
        }
    }

    public IReadOnlyCollection<FunctionAppDetails> GetCurrentSnapshot()
        => _functionApps.Values.ToList();
}