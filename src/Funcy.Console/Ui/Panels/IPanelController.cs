using Spectre.Console;

namespace Funcy.Console.Ui.Panels;

public interface IBodyPanelController
{
    Panel Panel { get; }
    void HandleInput(ConsoleKeyInfo keyInfo);
    void SwapFunction();
}