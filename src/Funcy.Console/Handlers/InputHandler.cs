using System.Collections.Concurrent;

namespace Funcy.Console.Handlers;

public class InputHandler
{
    private TaskCompletionSource _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    // Keys are queued rather than kept as a single latest value: a paste arrives as a fast burst of
    // key events, and the render loop cannot keep up one-at-a-time. Buffering keeps every key so a
    // pasted string is not collapsed to its first and last character.
    private readonly ConcurrentQueue<ConsoleKeyInfo> _queue = new();

    public bool IsTriggered { get; set; }

    public async Task StartListeningAsync(CancellationToken token)
    {
        await Task.Run(() =>
        {
            while (!token.IsCancellationRequested)
            {
                var key = System.Console.ReadKey(true);
                System.Console.SetCursorPosition(0, System.Console.CursorTop);

                _queue.Enqueue(key);
                IsTriggered = true;
                _tcs.TrySetResult();
            }
        }, token);
    }

    // Pulls the next buffered key. The render loop drains until this returns false.
    public bool TryDequeue(out ConsoleKeyInfo keyInfo) => _queue.TryDequeue(out keyInfo);

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
