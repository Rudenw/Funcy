using Funcy.Core.Model;

namespace Funcy.Console.Ui.Pagination.Matchers;

public class FunctionAppMatcher : ISearchMatcher<FunctionAppDetails>
{
    public bool TryMatch(FunctionAppDetails app, string input)
    {
        foreach (string searchTerm in input.Split(' '))
        {
            var match = false;
            match |= app.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase);
            match |= !string.IsNullOrWhiteSpace(app.System) && app.System.Contains(searchTerm, StringComparison.OrdinalIgnoreCase);
            match |= app.Functions.Any(f => f.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));

            if (!match)
            {
                return false;
            }
        }

        return true;
    }
}