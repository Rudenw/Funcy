using Funcy.Console.Ui.Navigation;
using Spectre.Console;

namespace Funcy.Console.Ui.Panels.Interfaces;

public interface IListPanel
{
    void HandleResize();
    Panel Panel { get; }
    void HandleInput(ConsoleKeyInfo keyInfo);
    void SetSearchText(string searchInputSearchText);
    bool TryGetNavigationRequest(out NavigationRequest? navigationRequest);
    bool TryGetActionNavigationRequest(out NavigationRequest? navigationRequest);
    Dictionary<TableIndex, ShortcutMap> GetShortcuts();
}