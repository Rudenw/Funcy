using System.Text;
using Funcy.Console.Ui.Input;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Funcy.Console.Ui.Panels;

public class TopPanel : IPanelController
{
    private readonly string _subscriptionName;
    private readonly StringBuilder _searchText = new();
    private bool _searchMode;
    private Table _table;
    public Panel Panel { get; }

    public TopPanel(string subscriptionName)
    {
        _subscriptionName = subscriptionName;
        Panel = new Panel(RenderLeftLayout());
        Panel.Border(BoxBorder.None);
    }

    private IRenderable RenderLeftLayout()
    {
        _table = new Table();
        _table.Border(TableBorder.None);
        _table.ShowHeaders = false;

        // Add some columns
        _table.AddColumn("col1");
        _table.AddColumn("col2");

        // Add some rows
        _table.AddRow(new Markup($"[bold yellow]Subscription:[/]"),
            new Markup($"{_subscriptionName}"));
        _table.AddRow(new Markup($"[bold purple_2]S[/][bold yellow]earch: [/]"), new Markup(_searchText.ToString()));
        
        return _table;
    }

    public void HandleInput(ConsoleKeyInfo keyInfo)
    {
        if (_searchMode)
        {
            switch (keyInfo.Key)
            {
                case ConsoleKey.Backspace:
                    if (_searchText.Length > 1)
                    {
                        _searchText.Remove(_searchText.Length - 2, 1);
                    }
                    
                    break;
                case ConsoleKey.Enter:
                    _searchMode = false;
                    break;
                default:
                    var keyToChar = TextInputInterpreter.Interpret(keyInfo) ?? null;
                    _searchText.Insert(_searchText.Length - 1, keyToChar);
                    break;
            }
        }
        else
        {
            switch (keyInfo.Key)
            {
                case ConsoleKey.S:
                    _searchMode = true;
                    _searchText.Append('_');
                    break;
            }
        }

        UpdateSearchCell();
    }
    
    private void UpdateSearchCell()
    {
        _table.Rows.Update(1, 1, new Markup(_searchText.ToString()));
    }
}