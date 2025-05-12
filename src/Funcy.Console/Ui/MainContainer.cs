using Funcy.Console.Ui.Panels;
using Funcy.Infrastructure.Model;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Funcy.Console.Ui;

public class MainContainer
{
    private TopPanel _topPanel;
    private FunctionAppPanel _functionListPanel;

    public MainContainer(string subscriptionName, List<FunctionAppDetails> functionApps)
    {
        _topPanel = new TopPanel(subscriptionName);
        _functionListPanel = new FunctionAppPanel(functionApps);
    }

    public IRenderable BuildMainLayout()
    {
        return new Rows(_topPanel.Panel, _functionListPanel.Panel);
    }

    public void HandleInput(ConsoleKey triggeredKey)
    {
        _topPanel.HandleInput(triggeredKey);
        _functionListPanel.HandleInput(triggeredKey);
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