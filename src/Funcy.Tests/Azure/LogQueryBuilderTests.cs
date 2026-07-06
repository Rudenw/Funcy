using Funcy.Infrastructure.Azure;
using Xunit;

namespace Funcy.Tests.Azure;

public class LogQueryBuilderTests
{
    [Fact]
    public void Build_UnionsAllThreeTables()
    {
        var query = LogQueryBuilder.Build("my-app", "my-func", since: null, maxRows: 200);

        Assert.Contains("union", query);
        Assert.Contains("(traces |", query);
        Assert.Contains("(exceptions |", query);
        Assert.Contains("(requests |", query);
    }

    [Fact]
    public void Build_FiltersByFunctionAndApp()
    {
        var query = LogQueryBuilder.Build("my-app", "my-func", since: null, maxRows: 200);

        Assert.Contains("operation_Name == 'my-func'", query);
        Assert.Contains("cloud_RoleName == 'my-app'", query);
    }

    [Fact]
    public void Build_OrdersNewestFirstAndCaps()
    {
        var query = LogQueryBuilder.Build("app", "func", since: null, maxRows: 150);

        Assert.Contains("order by timestamp desc", query);
        Assert.Contains("take 150", query);
    }

    [Fact]
    public void Build_WithoutSince_HasNoTimestampPredicate()
    {
        var query = LogQueryBuilder.Build("app", "func", since: null, maxRows: 200);

        Assert.DoesNotContain("where timestamp >", query);
    }

    [Fact]
    public void Build_WithSince_AddsIncrementalPredicate()
    {
        var since = new DateTimeOffset(2026, 7, 6, 12, 30, 0, TimeSpan.Zero);

        var query = LogQueryBuilder.Build("app", "func", since, maxRows: 200);

        Assert.Contains("where timestamp > datetime(2026-07-06T12:30:00", query);
    }

    [Fact]
    public void Build_EscapesSingleQuotesInNames()
    {
        var query = LogQueryBuilder.Build("ap'p", "fu'nc", since: null, maxRows: 10);

        // Embedded single quotes are escaped so they cannot break out of the KQL string literal.
        Assert.Contains(@"operation_Name == 'fu\'nc'", query);
        Assert.Contains(@"cloud_RoleName == 'ap\'p'", query);
    }
}
