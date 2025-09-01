using Funcy.Core.Model;

namespace Funcy.Console.Ui.Pagination.Matchers;

public class FunctionMatcher : ISearchMatcher<FunctionDetails>
{
    public bool TryMatch(FunctionDetails app, string input)
    {
        if (app.Name.Contains(input, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }
}