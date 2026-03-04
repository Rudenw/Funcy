using Funcy.Console.Ui.Pagination.Matchers;
using Funcy.Core.Model;

namespace Funcy.Tests.Matchers;

public class FunctionMatcherTests
{
    private readonly FunctionMatcher _sut = new();

    private static FunctionDetails MakeFunction(string name) =>
        new() { Name = name, FunctionAppName = "SomeApp", Trigger = "HttpTrigger" };

    [Fact]
    public void Match_WhenNameContainsInput()
    {
        Assert.True(_sut.TryMatch(MakeFunction("ProcessPayment"), "payment"));
    }

    [Fact]
    public void NoMatch_WhenNameDoesNotContainInput()
    {
        Assert.False(_sut.TryMatch(MakeFunction("ProcessPayment"), "invoice"));
    }

    [Fact]
    public void CaseInsensitive_MatchesUpperCase()
    {
        Assert.True(_sut.TryMatch(MakeFunction("ProcessPayment"), "PROCESS"));
    }

    [Fact]
    public void EmptyInput_ReturnsTrue()
    {
        Assert.True(_sut.TryMatch(MakeFunction("ProcessPayment"), ""));
    }
}
