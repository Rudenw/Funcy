using Funcy.Core.Model;

namespace Funcy.Console.Ui.Pagination.Matchers;

public class SubscriptionMatcher : ISearchMatcher<SubscriptionDetails>
{
    public bool TryMatch(SubscriptionDetails app, string input)
    {
        if (app.Name.Contains(input, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }
}