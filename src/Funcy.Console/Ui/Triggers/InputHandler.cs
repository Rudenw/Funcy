namespace Funcy.Console.Ui;

public class InputHandler
{
    private TaskCompletionSource _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public bool IsTriggered { get; set; }
    public ConsoleKey TriggeredKey { get; set; }

    public async Task StartListeningAsync(CancellationToken token)
    {
        await Task.Run(() =>
        {
            while (!token.IsCancellationRequested)
            {
                TriggeredKey = System.Console.ReadKey(true).Key;
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