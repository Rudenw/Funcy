using Funcy.Console.Ui.Input;
using Funcy.Core.Model;

namespace Funcy.Console.Handlers.Models;

public record DispatchedFunction(FunctionAction Action, FunctionAppDetails FunctionAppDetails, Task RunningTask);