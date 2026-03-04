using Funcy.Console.Ui.Input;

namespace Funcy.Console.Ui;

public interface IActionDispatcher
{
    Task Dispatch(InputActionResult inputResult);
}
