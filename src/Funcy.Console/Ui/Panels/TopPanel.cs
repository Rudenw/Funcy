using Spectre.Console;
using Funcy.Core.Model;


namespace Funcy.Console.Ui.Panels;

public class TopPanel
{
    private readonly string _subscriptionName;
    private readonly Table _table;
    public Panel Panel { get; }

    public TopPanel(string subscriptionName)
    {
        _subscriptionName = subscriptionName;
        
        _table = new Table();
        RenderTableLayout();
            
        Panel = new Panel(_table);
        Panel.Width = 104;
        Panel.Border(BoxBorder.Ascii);
    }

    private void RenderTableLayout()
    {
        _table.Border(TableBorder.None);
        _table.ShowHeaders = false;
        
        _table.AddColumn("", column => column.Width = 15);
        _table.AddColumn("", column =>
        {
            column.Width = 30;
            column.LeftAligned();
        });
        _table.AddColumn("", column =>
        {
            column.Width = 30;
            column.LeftAligned();
        });
        _table.AddColumn("", column =>
        {
            column.Width = 30;
            column.LeftAligned();
        });
        
        _table.AddRow(UiStyles.CreateLabelMarkup("Subscription:"),
            new Markup($"{_subscriptionName}"), UiStyles.CreateShortcutMarkup(Shortcuts.Filter.DisplayChar, Shortcuts.Filter.Label), UiStyles.CreateShortcutMarkup(Shortcuts.Swap.DisplayChar, Shortcuts.Swap.Label));
        _table.AddRow(UiStyles.CreateLabelMarkup("Filter:"), new Markup(""), UiStyles.CreateShortcutMarkup(Shortcuts.Start.DisplayChar, Shortcuts.Start.Label), UiStyles.CreateShortcutMarkup(Shortcuts.Stop.DisplayChar, Shortcuts.Stop.Label));
    }

    public void HandleInput(ConsoleKeyInfo keyInfo)
    {
    }
    
    public void SetSearchText(Markup searchMarkup)
    {
        UpdateSearchCell(searchMarkup);
    }

    public void FunctionAppState(FunctionState state)
    {
        // SwitchStartShortcut(state.CanStart());
        // SwitchStopShortcut(state.CanStop());
        // SwitchSwapShortcut(state.CanSwap());
    }
    
    private void UpdateSearchCell(Markup searchText)
    {
        _table.Rows.Update(1, 1, searchText);
    }

    private void SwitchStartShortcut(bool isEnabled)
    {
        _table.Rows.Update(1, 2,
            UiStyles.CreateShortcutMarkup(Shortcuts.Start.DisplayChar, Shortcuts.Start.Label, isEnabled));
    }
    
    private void SwitchStopShortcut(bool isEnabled)
    {
        _table.Rows.Update(1, 3,
            UiStyles.CreateShortcutMarkup(Shortcuts.Stop.DisplayChar, Shortcuts.Stop.Label, isEnabled));
    }
    
    private void SwitchSwapShortcut(bool isEnabled)
    {
        _table.Rows.Update(0, 3,
            UiStyles.CreateShortcutMarkup(Shortcuts.Start.DisplayChar, Shortcuts.Start.Label, isEnabled));
    }
}