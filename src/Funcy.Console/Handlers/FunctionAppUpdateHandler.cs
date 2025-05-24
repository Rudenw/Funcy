using System.Collections.Concurrent;
using Funcy.Core.Interfaces;
using Funcy.Core.Model;

namespace Funcy.Console.Handlers;

public class FunctionAppUpdateHandler
{
    private readonly IAzureFunctionService _functionService;
    private TaskCompletionSource _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public bool IsTriggered { get; private set; }
    private readonly Lock _lock = new();
    private readonly ConcurrentDictionary<string, FunctionAppDetails> _cache = [];
    private readonly List<FunctionAppDetails> _changedFunctionApps;

    public FunctionAppUpdateHandler(IAzureFunctionService functionService)
    {
        _functionService = functionService;
        _changedFunctionApps = functionService.GetFunctionsFromDatabase();
        foreach (var functionApp in _changedFunctionApps)
        {
            _cache.TryAdd(functionApp.Name, functionApp);
        }
    }

    public async Task StartListeningAsync(CancellationToken token)
    {
        await Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                var functionAppDetailsToUpdate = _functionService.FetchFunctionAppDetailsAsync(token);
                await UpdateFunctionAppList(functionAppDetailsToUpdate);
                
                await Task.Delay(TimeSpan.FromMinutes(5), token);
            }
        }, token);
    }

    private async Task UpdateFunctionAppList(IAsyncEnumerable<FunctionAppDetails> functionAppDetailsToUpdate)
    {
        await foreach (var newApp in functionAppDetailsToUpdate)
        {
            if (!_cache.TryGetValue(newApp.Name, out var old) || !newApp.Equals(old)) {
                lock (_lock)
                {
                    _changedFunctionApps.Add(newApp);
                }
                
            }

            IsTriggered = true;
            _tcs.TrySetResult();
        }
    }

    public Task WaitForTriggerAsync()
    {
        return _tcs.Task;
    }
    
    public List<FunctionAppDetails> ConsumeChanges()
    {
        lock(_lock)
        {
            var snapshot = new List<FunctionAppDetails>(_changedFunctionApps);
            _changedFunctionApps.Clear();
            IsTriggered = false;
            _tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            return snapshot;
        }
    }
}