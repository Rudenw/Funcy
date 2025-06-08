using Spectre.Console;

namespace Funcy.Console.Ui.Panels;

public interface IBodyPanelController
{
    Panel Panel { get; }
    public void SetSearchText(string searchText);
    void HandleInput(ConsoleKeyInfo keyInfo);
    void SwapFunction();
}