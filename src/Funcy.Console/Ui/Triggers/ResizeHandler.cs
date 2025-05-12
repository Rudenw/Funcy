namespace Funcy.Console.Ui.Triggers;

public class ResizeHandler
{
    private TaskCompletionSource _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public bool IsTriggered { get; private set; }

    public async Task StartPolling(CancellationToken cancellationToken, int interval = 500)
    {
        await Task.Run(async () =>
        {
            var lastSize = (System.Console.WindowWidth, System.Console.WindowHeight);
            while (!cancellationToken.IsCancellationRequested)
            {
                var currentSize = (System.Console.WindowWidth, System.Console.WindowHeight);

                if (lastSize != currentSize)
                {
                    lastSize = currentSize;
                    IsTriggered = true;
                    _tcs.TrySetResult();
                }

                await Task.Delay(interval, cancellationToken);
            }
        }, cancellationToken);
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