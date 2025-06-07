using Spectre.Console;

namespace Funcy.Console.Ui.Panels;

public interface IPanelController
{
    Panel Panel { get; }
    void HandleInput(ConsoleKeyInfo keyInfo);
}