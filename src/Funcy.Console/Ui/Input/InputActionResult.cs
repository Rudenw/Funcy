using Funcy.Infrastructure.Model;

namespace Funcy.Console.Input;

public record InputActionResult(FunctionAction Action, FunctionAppDetails FunctionAppDetails);

public enum FunctionAction
{
    Start,
    Stop,
    Swap,
}