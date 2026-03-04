using Funcy.Console.Ui.Pagination.Matchers;
using Funcy.Core.Model;
using Xunit;

namespace Funcy.Tests.Matchers;

public class FunctionAppSlotMatcherTests
{
    private readonly FunctionAppSlotMatcher _sut = new();

    private static FunctionAppSlotDetails MakeSlot(string name, string fullName) =>
        new() { Id = "slot-id", Name = name, FullName = fullName, State = FunctionState.Running };

    [Fact]
    public void Match_WhenNameContainsInput()
    {
        var slot = MakeSlot("staging", "MyApp/staging");
        Assert.True(_sut.TryMatch(slot, "stag"));
    }

    [Fact]
    public void Match_WhenFullNameContainsInput()
    {
        var slot = MakeSlot("prod", "MyApp (Production)");
        Assert.True(_sut.TryMatch(slot, "production"));
    }

    [Fact]
    public void NoMatch_WhenNeitherContainsInput()
    {
        var slot = MakeSlot("staging", "MyApp/staging");
        Assert.False(_sut.TryMatch(slot, "hotfix"));
    }

    [Fact]
    public void CaseInsensitive_MatchesUpperCase()
    {
        var slot = MakeSlot("staging", "MyApp/staging");
        Assert.True(_sut.TryMatch(slot, "STAGING"));
    }
}
