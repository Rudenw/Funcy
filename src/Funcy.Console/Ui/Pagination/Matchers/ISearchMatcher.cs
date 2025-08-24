namespace Funcy.Console.Ui.Pagination;

public interface ISearchMatcher<in T>
{
    bool TryMatch(T app, string input);
}