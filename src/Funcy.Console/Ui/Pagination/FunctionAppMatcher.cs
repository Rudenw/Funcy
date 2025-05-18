using Funcy.Console.Ui.Pagination.Models;
using Funcy.Infrastructure.Model;

namespace Funcy.Console.Ui.Pagination;

public static class FunctionAppMatcher
{
    public static MatchResult Match(FunctionAppDetails app, string input)
    {
        if (app.Name.Contains(input, StringComparison.OrdinalIgnoreCase))
            return new MatchResult { Source = MatchSource.Name, MatchedValue = app.Name };

        if (!string.IsNullOrWhiteSpace(app.System) && app.System.Contains(input, StringComparison.OrdinalIgnoreCase))
            return new MatchResult { Source = MatchSource.System, MatchedValue = app.System };

        foreach (var f in app.Functions)
        {
            if (f.Name.Contains(input, StringComparison.OrdinalIgnoreCase))
            {
                return new MatchResult
                {
                    Source = MatchSource.Function,
                    MatchedValue = f.Name
                };
            }
        }

        return new MatchResult { Source = MatchSource.None };
    }
}