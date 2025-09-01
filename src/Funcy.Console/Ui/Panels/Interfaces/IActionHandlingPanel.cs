using Funcy.Console.Ui.Input;
using Funcy.Core.Model;

namespace Funcy.Console.Ui.Panels.Interfaces;

public interface IActionHandlingPanel
{
    bool TryBuildAction(FunctionAction action, out InputActionResult? result);
}