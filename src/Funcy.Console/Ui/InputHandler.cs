namespace Funcy.Console.Ui;

public class InputHandler
{
    public int OldSelectedIndex { get; private set; }
    public int SelectedIndex { get; private set; }

    public TaskCompletionSource UpdateTrigger { get; private set; } = new();

    public int VisibleStartIndex { get; private set; }
    public bool IsVisibleStartIndexChanged { get; private set; }

    public async Task HandleInputAsync(int maxVisibleRows, int rowCount, CancellationToken token)
    {
        await Task.Run(() =>
        {
            while (!token.IsCancellationRequested)
            {
                var key = System.Console.ReadKey(true).Key;
                OldSelectedIndex = SelectedIndex;
                IsVisibleStartIndexChanged = false;
                System.Console.SetCursorPosition(0, System.Console.CursorTop);
                switch (key)
                {
                    case ConsoleKey.UpArrow:
                        SelectedIndex--;
                        
                        if (SelectedIndex < 0 && VisibleStartIndex > 0)
                        {
                            IsVisibleStartIndexChanged = true;
                            VisibleStartIndex--;
                            SelectedIndex = 0;
                        }

                        if (SelectedIndex < 0)
                        {
                            SelectedIndex = 0;
                        }
                        
                        break;
                    case ConsoleKey.DownArrow:
                        SelectedIndex++;

                        if (SelectedIndex >= maxVisibleRows && SelectedIndex + VisibleStartIndex < rowCount)
                        {
                            IsVisibleStartIndexChanged = true;
                            VisibleStartIndex++;
                            SelectedIndex = maxVisibleRows - 1;
                        }

                        if (SelectedIndex >= maxVisibleRows)
                        {
                            SelectedIndex = maxVisibleRows - 1;
                        }
                        
                        break;
                    case ConsoleKey.Escape:
                        token.ThrowIfCancellationRequested();
                        break;
                }

                CompleteUpdateTrigger();
            }
        }, token);
    }

    private void CompleteUpdateTrigger()
    {
        lock (UpdateTrigger)
        {
            UpdateTrigger.TrySetResult();
        }
    }

    public void ResetUpdateTrigger()
    {
        lock (UpdateTrigger)
        {
            UpdateTrigger = new TaskCompletionSource();
        }
    }
}