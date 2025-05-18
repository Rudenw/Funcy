using System.Text;
using Funcy.Console.Ui.Panels;
using Funcy.Infrastructure.Model;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Funcy.Console.Ui;

public class MainContainer(string subscriptionName, List<FunctionAppDetails> functionApps)
{
    private readonly TopPanel _topPanel = new(subscriptionName);
    private readonly FunctionAppPanel _functionListPanel = new(functionApps);
    private bool _searchMode;
    private readonly StringBuilder _searchText = new();
    private int _searchIndex;

    public IRenderable BuildMainLayout()
    {
        return new Rows(_topPanel.Panel, _functionListPanel.Panel);
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
                        _searchIndex = _searchText.Length - 1;
                    }
                    break;
                case ConsoleKey.Enter:
                    _searchMode = false;
                    _searchText.Remove(_searchText.Length - 1, 1);
                    break;
                case ConsoleKey.LeftArrow:
                    _searchIndex = Math.Max(0, _searchIndex - 1);
                    break;
                case ConsoleKey.RightArrow:
                    _searchIndex = Math.Min(_searchText.Length - 1, _searchIndex + 1);
                    break;
                case ConsoleKey.Delete:
                    if (_searchIndex < _searchText.Length - 1)
                    {
                        _searchText.Remove(_searchIndex, 1);
                    }
                    
                    break;
                default:
                    var keyToChar = Interpret(keyInfo) ?? null;
                    _searchText.Insert(_searchIndex, keyToChar);
                    _searchIndex++;
                    break;
            }
        }
        else
        {
            switch (keyInfo.Key)
            {
                case ConsoleKey.S:
                    _searchMode = true;
                    _searchText.Append(' ');
                    break;
                case ConsoleKey.Delete:
                    _searchText.Clear();
                    _searchIndex = 0;
                    break;
            }
        }
        
        _topPanel.SetSearchText(GetMarkup());
        _functionListPanel.SetSearchText(_searchText.ToString());
        _functionListPanel.HandleInput(keyInfo);
    }

    private Markup GetMarkup()
    {
        var markupText = _searchText.ToString();

        if (string.IsNullOrEmpty(markupText))
        {
            return new Markup(markupText);
        }
        
        if (!_searchMode)
        {
            markupText += " [bold red]del[/]";
        }
        else
        {
            markupText = markupText[.._searchIndex]
                         + "[underline]"
                         + markupText[_searchIndex]
                         + "[/]"
                         + markupText[(_searchIndex + 1)..];
        }
        
        return new Markup(markupText);
    }
    
    private char? Interpret(ConsoleKeyInfo keyInfo)
    {
        return !char.IsControl(keyInfo.KeyChar) 
            ? keyInfo.KeyChar 
            : null;
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