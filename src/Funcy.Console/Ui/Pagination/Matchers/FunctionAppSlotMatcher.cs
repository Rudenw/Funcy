using Funcy.Core.Model;

namespace Funcy.Console.Ui.Pagination.Matchers;

public class FunctionAppSlotMatcher : ISearchMatcher<FunctionAppSlotDetails>
{
    public bool TryMatch(FunctionAppSlotDetails app, string input)
    {
        if (app.Name.Contains(input, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(app.FullName) && app.FullName.Contains(input, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }
}