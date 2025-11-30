namespace Funcy.Console.Ui.ConsoleHelper;

public static class ConsoleKeyHelper
{
    public static int? TryGetDigit(ConsoleKey key)
    {
        return key switch
        {
            >= ConsoleKey.D0 and <= ConsoleKey.D9 => (int)key - (int)ConsoleKey.D0,
            >= ConsoleKey.NumPad0 and <= ConsoleKey.NumPad9 => (int)key - (int)ConsoleKey.NumPad0,
            _ => null
        };
    }
}