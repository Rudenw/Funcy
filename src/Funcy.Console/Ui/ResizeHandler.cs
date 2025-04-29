using System.Runtime.InteropServices;

namespace Funcy.Console.Ui;

public class ResizeHandler
{
    private int _previousWidth = System.Console.WindowWidth;
    private int _previousHeight = System.Console.WindowHeight;
    private readonly CancellationTokenSource _cts = new();

    public event Action<int, int> OnResize;

    public void StartPolling(int interval = 500)
    {
        Task.Run(async () =>
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                var currentWidth = System.Console.WindowWidth;
                var currentHeight = System.Console.WindowHeight;

                if (currentWidth != _previousWidth || currentHeight != _previousHeight)
                {
                    _previousWidth = currentWidth;
                    _previousHeight = currentHeight;
                    
                    OnResize?.Invoke(currentWidth, currentHeight);
                }

                await Task.Delay(interval, _cts.Token);
            }
        }, _cts.Token);
    }

    public void StopPolling()
    {
        _cts.Cancel();
    }
}