using Funcy.Core.Model;

namespace Funcy.Console.Ui.Input;

public record InputActionResult(FunctionAction Action, FunctionAppDetails FunctionAppDetails);

public enum FunctionAction
{
    Start,
    Stop,
    Swap,
}

public static class FunctionActionExtensions
{
    public static string GetActivatingState(this FunctionAction action)
    {
        return action switch
        {
            FunctionAction.Start => "Starting...",
            FunctionAction.Stop => "Stopping...",
            FunctionAction.Swap => "Swapping...",
            _ => throw new ArgumentOutOfRangeException(nameof(action), action, null)
        };
    }
}