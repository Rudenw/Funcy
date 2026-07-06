using Funcy.Core.Model;
using Xunit;

namespace Funcy.Tests.Model;

public class LogEntryDetailsTests
{
    private static LogEntryDetails Make(DateTimeOffset ts, string key) => new()
    {
        Timestamp = ts,
        ItemType = LogItemType.Trace,
        Message = "m",
        Key = key,
    };

    [Fact]
    public void Sort_OrdersNewestFirst()
    {
        var older = Make(new DateTimeOffset(2026, 7, 6, 10, 0, 0, TimeSpan.Zero), "a");
        var newer = Make(new DateTimeOffset(2026, 7, 6, 11, 0, 0, TimeSpan.Zero), "b");

        var list = new List<LogEntryDetails> { older, newer };
        list.Sort();

        Assert.Same(newer, list[0]);
        Assert.Same(older, list[1]);
    }

    [Fact]
    public void CompareTo_SameTimestamp_TieBreaksByKey()
    {
        var ts = new DateTimeOffset(2026, 7, 6, 10, 0, 0, TimeSpan.Zero);
        var a = Make(ts, "a");
        var b = Make(ts, "b");

        Assert.True(a.CompareTo(b) < 0);
    }

    [Fact]
    public void BuildKey_UsesItemIdWhenPresent()
    {
        var key = LogEntryDetails.BuildKey("item-1", DateTimeOffset.UtcNow, LogItemType.Trace, "msg");

        Assert.Equal("item-1", key);
    }

    [Fact]
    public void BuildKey_FallsBackToTimestampAndHash_WhenItemIdMissing()
    {
        var ts = new DateTimeOffset(2026, 7, 6, 10, 0, 0, TimeSpan.Zero);

        var key1 = LogEntryDetails.BuildKey(null, ts, LogItemType.Trace, "same");
        var key2 = LogEntryDetails.BuildKey("", ts, LogItemType.Trace, "same");
        var different = LogEntryDetails.BuildKey(null, ts, LogItemType.Exception, "same");

        Assert.Equal(key1, key2);            // stable for identical inputs
        Assert.NotEqual(key1, different);    // item type differentiates
    }
}
