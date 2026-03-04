using Funcy.Console.Ui.Pagination.Matchers;
using Funcy.Core.Model;
using Xunit;

namespace Funcy.Tests.Matchers;

public class FunctionAppMatcherTests
{
    private readonly FunctionAppMatcher _sut = new(["System"]);

    private static FunctionAppDetails MakeApp(
        string name,
        string system = "SysA",
        string[]? functionNames = null) =>
        new()
        {
            Name = name,
            State = FunctionState.Running,
            Tags = {
                { "System", system }
            },
            ResourceGroup = "rg-test",
            Subscription = "sub-test",
            Id = "id-test",
            Functions = (functionNames ?? [])
                .Select(fn => new FunctionDetails
                {
                    Name = fn,
                    FunctionAppName = name,
                    Trigger = "HttpTrigger"
                })
                .ToList()
        };

    [Fact]
    public void EmptySearch_MatchesAnyApp()
    {
        var app = MakeApp("PaymentApp");
        Assert.True(_sut.TryMatch(app, ""));
    }

    [Fact]
    public void SingleTerm_MatchesOnAppName()
    {
        var app = MakeApp("PaymentApp");
        Assert.True(_sut.TryMatch(app, "payment"));
    }

    [Fact]
    public void SingleTerm_MatchesOnSystemName()
    {
        var app = MakeApp("OtherApp", system: "Billing");
        Assert.True(_sut.TryMatch(app, "billing"));
    }

    [Fact]
    public void SingleTerm_MatchesOnFunctionName()
    {
        var app = MakeApp("OtherApp", functionNames: ["ProcessInvoice"]);
        Assert.True(_sut.TryMatch(app, "invoice"));
    }

    [Fact]
    public void MultipleTerms_AllMatch_ReturnsTrue()
    {
        var app = MakeApp("PaymentApp", system: "Billing");
        Assert.True(_sut.TryMatch(app, "payment billing"));
    }

    [Fact]
    public void MultipleTerms_OneFails_ReturnsFalse()
    {
        var app = MakeApp("PaymentApp", system: "Billing");
        Assert.False(_sut.TryMatch(app, "payment orders"));
    }

    [Fact]
    public void CaseInsensitive_MatchesUpperCase()
    {
        var app = MakeApp("PaymentApp");
        Assert.True(_sut.TryMatch(app, "PAYMENT"));
    }

    [Fact]
    public void NoMatch_ReturnsFalse()
    {
        var app = MakeApp("PaymentApp", system: "Billing");
        Assert.False(_sut.TryMatch(app, "inventory"));
    }
}
