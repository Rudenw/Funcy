namespace Funcy.Console.Ui.Pagination.Matchers;

public interface ISearchMatcher<in T>
{
    bool TryMatch(T app, string input);
}