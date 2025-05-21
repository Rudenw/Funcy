using Funcy.Console.Input;
using Funcy.Console.Ui.Panels;
using Funcy.Infrastructure.Model;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Funcy.Console.Ui;

public class MainContainer(string subscriptionName, List<FunctionAppDetails> functionApps)
{
    private readonly TopPanel _topPanel = new(subscriptionName);
    private readonly FunctionAppPanel _functionListPanel = new(functionApps);
    private readonly SearchInputManager _searchInput = new();

    public IRenderable BuildMainLayout()
    {
        return new Rows(_topPanel.Panel, _functionListPanel.Panel);
    }

    public InputActionResult? HandleInput(ConsoleKeyInfo keyInfo)
    {
        var action = _searchInput.HandleInput(keyInfo);

        if (action is not null)
        {
            var selectedFunctionAppDetails = _functionListPanel.GetSelectedFunctionAppDetails();
            return new InputActionResult(action.GetValueOrDefault(), selectedFunctionAppDetails);
        }

        _topPanel.SetSearchText(_searchInput.SearchMarkup);
        _functionListPanel.SetSearchText(_searchInput.SearchText);
        _functionListPanel.HandleInput(keyInfo);

        return null;
    }

    public void UpdateData(List<FunctionAppDetails> functionApps)
    {
        _functionListPanel.UpdateData(functionApps);
    }

    public void HandleResize()
    {
        _functionListPanel.HandleResize();
    }
}