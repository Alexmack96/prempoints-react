using Api.Infrastructure;

namespace UnitTests;

/// <summary>
/// The schedule decides when the database is awake, and awake time is what the
/// free Azure SQL allowance is spent on. An off-by-one here is a bill or an
/// outage, so the boundaries are worth pinning down.
/// </summary>
public class DatabaseKeepAliveOptionsTests
{
    private static DatabaseKeepAliveOptions Weekend()
    {
        var options = new DatabaseKeepAliveOptions { TimeZone = "Europe/London" };
        var window = new KeepAliveWindow { Start = "06:00", End = "22:00" };

        window.Days.Add(DayOfWeek.Friday);
        window.Days.Add(DayOfWeek.Saturday);
        window.Days.Add(DayOfWeek.Sunday);
        options.Windows.Add(window);

        return options;
    }

    [Theory]
    // Saturday 14:00 UTC is 15:00 in London during summer time: inside.
    [InlineData("2026-08-22T14:00:00Z", true)]
    // Friday 06:00 local, the first minute of the window.
    [InlineData("2026-08-21T05:00:00Z", true)]
    // Friday 05:59 local, the minute before it opens.
    [InlineData("2026-08-21T04:59:00Z", false)]
    // Sunday 22:00 local. The end is exclusive, so this is shut.
    [InlineData("2026-08-23T21:00:00Z", false)]
    // Sunday 21:59 local, the last minute inside.
    [InlineData("2026-08-23T20:59:00Z", true)]
    // Wednesday afternoon: no window at all.
    [InlineData("2026-08-19T14:00:00Z", false)]
    public void WindowBoundariesAreRespected(string instant, bool expected)
    {
        var options = Weekend();

        var actual = options.IsInsideWindow(DateTimeOffset.Parse(instant, null));

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void WindowsAreJudgedInLocalTimeAcrossADaylightSavingChange()
    {
        var options = Weekend();

        // 06:30 UTC on the Saturday after the UK clocks go back is 06:30 local,
        // so it is inside. The same instant in summer would be 07:30 local and
        // also inside — the case that matters is the one an hour earlier.
        Assert.True(options.IsInsideWindow(DateTimeOffset.Parse("2026-10-31T06:30:00Z", null)));

        // 05:30 UTC in winter is 05:30 local: shut. In summer the same clock
        // reading would have been 06:30 local and open, which is exactly the
        // drift that using UTC directly would have introduced.
        Assert.False(options.IsInsideWindow(DateTimeOffset.Parse("2026-10-31T05:30:00Z", null)));
    }

    [Fact]
    public void NoWindowsMeansNeverAwake()
    {
        var options = new DatabaseKeepAliveOptions();

        Assert.False(options.IsInsideWindow(DateTimeOffset.Parse("2026-08-22T14:00:00Z", null)));
    }

    [Fact]
    public void AMalformedTimeClosesTheWindowRatherThanThrowing()
    {
        var options = new DatabaseKeepAliveOptions();
        var window = new KeepAliveWindow { Start = "half past six", End = "22:00" };
        window.Days.Add(DayOfWeek.Saturday);
        options.Windows.Add(window);

        // A typo costs the schedule, not the process. This runs on a timer
        // inside a background service, where an exception stops the host.
        Assert.False(options.IsInsideWindow(DateTimeOffset.Parse("2026-08-22T14:00:00Z", null)));
    }

    [Fact]
    public void AnUnknownTimeZoneFallsBackToUtcRatherThanThrowing()
    {
        var options = Weekend();
        options.TimeZone = "Mars/Olympus_Mons";

        // Saturday 14:00 UTC is inside the window read as UTC. The point is
        // that it answers at all: a mistyped zone must not take the app down.
        Assert.True(options.IsInsideWindow(DateTimeOffset.Parse("2026-08-22T14:00:00Z", null)));
    }
}
