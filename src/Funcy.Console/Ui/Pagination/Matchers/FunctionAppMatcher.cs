using Funcy.Console.Ui.Pagination.Models;
using Funcy.Core.Model;

namespace Funcy.Console.Ui.Pagination;

public class FunctionAppMatcher : ISearchMatcher<FunctionAppDetails>
{
    public bool TryMatch(FunctionAppDetails app, string input)
    {
        if (app.Name.Contains(input, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(app.System) && app.System.Contains(input, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return app.Functions.Any(f => f.Name.Contains(input, StringComparison.OrdinalIgnoreCase));
    }
}