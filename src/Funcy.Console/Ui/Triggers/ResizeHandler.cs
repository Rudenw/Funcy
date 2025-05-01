namespace Funcy.Console.Ui.Triggers;

public class ResizeHandler
{
    private readonly CancellationTokenSource _cts = new();
    private TaskCompletionSource _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public bool IsTriggered { get; private set; }

    public void StartPolling(int interval = 500)
    {
        Task.Run(async () =>
        {
            var lastSize = (System.Console.WindowWidth, System.Console.WindowHeight);
            while (!_cts.Token.IsCancellationRequested)
            {
                var currentSize = (System.Console.WindowWidth, System.Console.WindowHeight);

                if (lastSize != currentSize)
                {
                    lastSize = currentSize;
                    IsTriggered = true;
                    _tcs.TrySetResult();
                }

                await Task.Delay(interval, _cts.Token);
            }
        }, _cts.Token);
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

    public void StopPolling()
    {
        _cts.Cancel();
    }
}