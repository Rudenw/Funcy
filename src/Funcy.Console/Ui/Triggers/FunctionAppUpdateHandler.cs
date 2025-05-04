using Funcy.Infrastructure.Azure;
using Funcy.Infrastructure.Model;

namespace Funcy.Console.Ui.Triggers;

public class FunctionAppUpdateHandler(AzureFunctionService functionService)
{
    private TaskCompletionSource _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public bool IsTriggered { get; set; }
    public readonly List<FunctionAppDetails> FunctionApps = functionService.GetFunctionsFromDatabase();

    public async Task StartListeningAsync(CancellationToken token)
    {
        await Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                await foreach (var newApp in functionService.FetchFunctionAppDetailsAsync(token))
                {
                    lock (FunctionApps)
                    {
                        var existing = FunctionApps.FirstOrDefault(x => x.Name == newApp.Name);
                        if (existing is not null)
                        {
                            FunctionApps.Remove(existing);
                        }

                        FunctionApps.Add(newApp);
                    }

                    IsTriggered = true;
                    _tcs.TrySetResult();
                }
                
                await Task.Delay(TimeSpan.FromMinutes(5), token);
            }
        }, token);
    }
    
    public Task WaitForTriggerAsync()
    {
        return _tcs.Task;
    }
    
    public void ResetTrigger()
    {
        IsTriggered = false;
        _tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}