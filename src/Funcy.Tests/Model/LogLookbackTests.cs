using Funcy.Core.Model;
using Xunit;

namespace Funcy.Tests.Model;

public class LogLookbackTests
{
    [Theory]
    [InlineData(LogLookback.OneHour, LogLookback.SixHours)]
    [InlineData(LogLookback.SixHours, LogLookback.TwelveHours)]
    [InlineData(LogLookback.TwelveHours, LogLookback.TwentyFourHours)]
    [InlineData(LogLookback.TwentyFourHours, LogLookback.ThreeDays)]
    [InlineData(LogLookback.ThreeDays, LogLookback.SevenDays)]
    [InlineData(LogLookback.SevenDays, LogLookback.ThirtyDays)]
    public void Longer_StepsUp(LogLookback current, LogLookback expected)
    {
        Assert.Equal(expected, current.Longer());
    }

    [Theory]
    [InlineData(LogLookback.ThirtyDays, LogLookback.SevenDays)]
    [InlineData(LogLookback.SevenDays, LogLookback.ThreeDays)]
    [InlineData(LogLookback.ThreeDays, LogLookback.TwentyFourHours)]
    [InlineData(LogLookback.TwentyFourHours, LogLookback.TwelveHours)]
    [InlineData(LogLookback.TwelveHours, LogLookback.SixHours)]
    [InlineData(LogLookback.SixHours, LogLookback.OneHour)]
    public void Shorter_StepsDown(LogLookback current, LogLookback expected)
    {
        Assert.Equal(expected, current.Shorter());
    }

    [Fact]
    public void Longer_ClampsAtWidest()
    {
        Assert.Equal(LogLookback.ThirtyDays, LogLookback.ThirtyDays.Longer());
    }

    [Fact]
    public void Shorter_ClampsAtNarrowest()
    {
        Assert.Equal(LogLookback.OneHour, LogLookback.OneHour.Shorter());
    }

    [Theory]
    [InlineData(LogLookback.OneHour, 1)]
    [InlineData(LogLookback.SixHours, 6)]
    [InlineData(LogLookback.TwelveHours, 12)]
    [InlineData(LogLookback.TwentyFourHours, 24)]
    public void ToTimeSpan_Hours(LogLookback lookback, int hours)
    {
        Assert.Equal(TimeSpan.FromHours(hours), lookback.ToTimeSpan());
    }

    [Theory]
    [InlineData(LogLookback.ThreeDays, 3)]
    [InlineData(LogLookback.SevenDays, 7)]
    [InlineData(LogLookback.ThirtyDays, 30)]
    public void ToTimeSpan_Days(LogLookback lookback, int days)
    {
        Assert.Equal(TimeSpan.FromDays(days), lookback.ToTimeSpan());
    }
}
