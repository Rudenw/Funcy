using Spectre.Console;
using Spectre.Console.Rendering;

namespace Funcy.Console.Ui.Panels;

public class TopPanel : IPanelController
{
    public Panel Panel { get; set; }

    public TopPanel(string subscriptionName)
    {
        var text = new Markup($"[bold yellow]Subscription: [/] {subscriptionName}");
        var rows = new Rows(text);
        Panel = new Panel(rows);
        Panel.Border(BoxBorder.None);
    }
}