namespace Funcy.Console.Handlers;

public class InputHandler
{
    private TaskCompletionSource _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public bool IsTriggered { get; set; }
    public ConsoleKeyInfo TriggeredKeyInfo { get; set; }

    public async Task StartListeningAsync(CancellationToken token)
    {
        await Task.Run(() =>
        {
            while (!token.IsCancellationRequested)
            {
                TriggeredKeyInfo = System.Console.ReadKey(true);
                System.Console.SetCursorPosition(0, System.Console.CursorTop);
                
                IsTriggered = true;
                _tcs.TrySetResult();
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