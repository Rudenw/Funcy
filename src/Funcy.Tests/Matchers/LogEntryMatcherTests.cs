using Funcy.Console.Ui.Pagination.Matchers;
using Funcy.Core.Model;
using Xunit;

namespace Funcy.Tests.Matchers;

public class LogEntryMatcherTests
{
    private readonly LogEntryMatcher _sut = new();

    private static LogEntryDetails MakeEntry(
        string message = "Processing order 12345",
        LogItemType type = LogItemType.Trace,
        string? severity = "Info") =>
        new()
        {
            Timestamp = DateTimeOffset.UtcNow,
            ItemType = type,
            Severity = severity,
            Message = message,
            Key = Guid.NewGuid().ToString(),
        };

    [Fact]
    public void Match_WhenMessageContainsInput()
    {
        Assert.True(_sut.TryMatch(MakeEntry(), "12345"));
    }

    [Fact]
    public void Match_CaseInsensitiveMessage()
    {
        Assert.True(_sut.TryMatch(MakeEntry("Order SHIPPED"), "shipped"));
    }

    [Fact]
    public void Match_WhenItemTypeMatches()
    {
        Assert.True(_sut.TryMatch(MakeEntry(type: LogItemType.Exception), "exception"));
    }

    [Fact]
    public void Match_WhenSeverityMatches()
    {
        Assert.True(_sut.TryMatch(MakeEntry(severity: "Warning"), "warning"));
    }

    [Fact]
    public void NoMatch_WhenNothingContainsInput()
    {
        Assert.False(_sut.TryMatch(MakeEntry(), "invoice"));
    }

    [Fact]
    public void EmptyInput_ReturnsTrue()
    {
        Assert.True(_sut.TryMatch(MakeEntry(), ""));
    }
}
