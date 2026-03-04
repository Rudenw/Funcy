using Funcy.Console.Ui.Pagination.Matchers;
using Funcy.Core.Model;

namespace Funcy.Tests.Matchers;

public class SubscriptionMatcherTests
{
    private readonly SubscriptionMatcher _sut = new();

    private static SubscriptionDetails MakeSub(string name) =>
        new() { Name = name, Id = "sub-id" };

    [Fact]
    public void Match_WhenNameContainsInput()
    {
        Assert.True(_sut.TryMatch(MakeSub("Production-Sub"), "prod"));
    }

    [Fact]
    public void NoMatch_WhenNameDoesNotContainInput()
    {
        Assert.False(_sut.TryMatch(MakeSub("Production-Sub"), "staging"));
    }

    [Fact]
    public void CaseInsensitive_Match()
    {
        Assert.True(_sut.TryMatch(MakeSub("Production-Sub"), "PRODUCTION"));
    }
}
