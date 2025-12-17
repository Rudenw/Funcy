using Funcy.Console.Ui.Controllers;
using Funcy.Console.Ui.Input;
using Funcy.Console.Ui.Panels.Interfaces;

namespace Funcy.Console.Ui.Contexts;

public class ListPanelContext
{
    public SearchInputManager SearchInputManager { get; } = new();
    public required IListController Controller { get; set; }
    public required IListPanel View { get; init; }
}