using System.Text;
using Spectre.Console;

namespace Funcy.Console.Ui.Input;

public class SearchInputManager
{
    private bool _searchMode;
    private readonly StringBuilder _searchText = new();
    private int _searchIndex;

    public string SearchText => _searchText.ToString();
    public Markup SearchMarkup => GetMarkup();

    public FunctionAction? HandleInput(ConsoleKeyInfo keyInfo)
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
                    var keyToChar = Interpret(keyInfo);
                    if (keyToChar != null)
                    {
                        _searchText.Insert(_searchIndex, keyToChar);
                        _searchIndex++;
                    }
                    break;
            }
        }
        else
        {
            switch (keyInfo.Key)
            {
                case var key when key == Shortcuts.Filter.Key:
                    _searchMode = true;
                    _searchText.Append(' ');
                    break;
                case var key when key == Shortcuts.Start.Key:
                    return FunctionAction.Start;
                case var key when key == Shortcuts.Stop.Key:
                    return FunctionAction.Stop;
                case var key when key == Shortcuts.Swap.Key:
                    return FunctionAction.Swap;
                case ConsoleKey.Delete:
                    _searchText.Clear();
                    _searchIndex = 0;
                    break;
            }
        }

        return null;
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
            markupText += " " + UiStyles.CreateDangerText("del");
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
        return !char.IsControl(keyInfo.KeyChar) ? keyInfo.KeyChar : null;
    }
}
