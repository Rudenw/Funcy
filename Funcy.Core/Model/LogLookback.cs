namespace Funcy.Core.Model;

// How far back the initial log query reaches. Cycled by the user; a smaller window keeps the
// service-side scan cheap, a larger one surfaces older telemetry.
public enum LogLookback
{
    OneHour,
    SixHours,
    TwelveHours,
    TwentyFourHours,
    ThreeDays,
    SevenDays,
    ThirtyDays,
}

public static class LogLookbackExtensions
{
    public static TimeSpan ToTimeSpan(this LogLookback lookback) => lookback switch
    {
        LogLookback.OneHour => TimeSpan.FromHours(1),
        LogLookback.SixHours => TimeSpan.FromHours(6),
        LogLookback.TwelveHours => TimeSpan.FromHours(12),
        LogLookback.TwentyFourHours => TimeSpan.FromHours(24),
        LogLookback.ThreeDays => TimeSpan.FromDays(3),
        LogLookback.SevenDays => TimeSpan.FromDays(7),
        LogLookback.ThirtyDays => TimeSpan.FromDays(30),
        _ => TimeSpan.FromHours(1),
    };

    // A longer window (next step up), clamped at the widest (30d) — used by the "range +" key.
    public static LogLookback Longer(this LogLookback lookback) => lookback switch
    {
        LogLookback.OneHour => LogLookback.SixHours,
        LogLookback.SixHours => LogLookback.TwelveHours,
        LogLookback.TwelveHours => LogLookback.TwentyFourHours,
        LogLookback.TwentyFourHours => LogLookback.ThreeDays,
        LogLookback.ThreeDays => LogLookback.SevenDays,
        LogLookback.SevenDays => LogLookback.ThirtyDays,
        _ => LogLookback.ThirtyDays,
    };

    // A shorter window (next step down), clamped at the narrowest (1h) — used by the "range -" key.
    public static LogLookback Shorter(this LogLookback lookback) => lookback switch
    {
        LogLookback.ThirtyDays => LogLookback.SevenDays,
        LogLookback.SevenDays => LogLookback.ThreeDays,
        LogLookback.ThreeDays => LogLookback.TwentyFourHours,
        LogLookback.TwentyFourHours => LogLookback.TwelveHours,
        LogLookback.TwelveHours => LogLookback.SixHours,
        LogLookback.SixHours => LogLookback.OneHour,
        _ => LogLookback.OneHour,
    };

    public static string ToDisplayLabel(this LogLookback lookback) => lookback switch
    {
        LogLookback.OneHour => "1h",
        LogLookback.SixHours => "6h",
        LogLookback.TwelveHours => "12h",
        LogLookback.TwentyFourHours => "24h",
        LogLookback.ThreeDays => "3d",
        LogLookback.SevenDays => "7d",
        LogLookback.ThirtyDays => "30d",
        _ => "1h",
    };
}
