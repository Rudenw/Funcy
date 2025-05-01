using Spectre.Console;
using Spectre.Console.Rendering;

namespace Funcy.Console.Ui.Panels;

public class TopPanel : IPanelController
{
    public Panel Panel { get; set; }

    public TopPanel()
    {
        var text = new Markup("[bold yellow]Subscription: [/] subscription-name");
        var text2 = new Markup("[bold yellow]Resource Group: [/] resource-group");
        var rows = new Rows(text, text2);
        Panel = new Panel(rows);
        Panel.Border(BoxBorder.None);
    }
}