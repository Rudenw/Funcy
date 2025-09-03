using Funcy.Console.Ui.Controllers;
using Funcy.Console.Ui.Input;
using Funcy.Console.Ui.Panels.Interfaces;

namespace Funcy.Console.Ui.Contexts;

public class ListPanelContext
{
    public SearchInputManager SearchInputManager { get; } = new();
    public IListController Controller { get; set; } = null!;
    public IListPanel View { get; set; } = null!;
}