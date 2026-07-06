using Funcy.Core.Model;
using Xunit;

namespace Funcy.Tests.Model;

public class LogTypeFilterTests
{
    [Theory]
    [InlineData(LogTypeFilter.All, LogTypeFilter.Traces)]
    [InlineData(LogTypeFilter.Traces, LogTypeFilter.Exceptions)]
    [InlineData(LogTypeFilter.Exceptions, LogTypeFilter.Requests)]
    [InlineData(LogTypeFilter.Requests, LogTypeFilter.All)]
    public void Next_CyclesInOrder(LogTypeFilter current, LogTypeFilter expected)
    {
        Assert.Equal(expected, current.Next());
    }

    [Fact]
    public void Next_FullCycleReturnsToStart()
    {
        var filter = LogTypeFilter.All;
        for (var i = 0; i < 4; i++)
        {
            filter = filter.Next();
        }

        Assert.Equal(LogTypeFilter.All, filter);
    }

    [Theory]
    [InlineData(LogTypeFilter.All, LogItemType.Trace, true)]
    [InlineData(LogTypeFilter.All, LogItemType.Exception, true)]
    [InlineData(LogTypeFilter.Traces, LogItemType.Trace, true)]
    [InlineData(LogTypeFilter.Traces, LogItemType.Exception, false)]
    [InlineData(LogTypeFilter.Exceptions, LogItemType.Exception, true)]
    [InlineData(LogTypeFilter.Exceptions, LogItemType.Request, false)]
    [InlineData(LogTypeFilter.Requests, LogItemType.Request, true)]
    [InlineData(LogTypeFilter.Requests, LogItemType.Trace, false)]
    public void Includes_MatchesItemTypeAgainstFilter(LogTypeFilter filter, LogItemType type, bool expected)
    {
        Assert.Equal(expected, filter.Includes(type));
    }
}
