using Funcy.Core.Model;

namespace Funcy.Console.Ui.Pagination.Matchers;

public class FunctionAppMatcher(string[] tagColumns) : ISearchMatcher<FunctionAppDetails>
{
    public bool TryMatch(FunctionAppDetails app, string input)
    {
        foreach (var searchTerm in input.Split(' '))
        {
            var match = false;
            match |= app.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase);
            foreach (var tagColumn in tagColumns)
            {
                var value = app.Tags.TryGetValue(tagColumn, out var v) ? v : string.Empty;
                match |= !string.IsNullOrWhiteSpace(value) && value.Contains(searchTerm, StringComparison.OrdinalIgnoreCase);
            }
            
            match |= app.Functions.Any(f => f.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));

            if (!match)
            {
                return false;
            }
        }

        return true;
    }
}