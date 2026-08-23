using System.Globalization;

namespace Api.Infrastructure;

/// <summary>
/// When to hold the database awake.
/// <para>
/// A schedule rather than a plain interval, because the free Azure SQL offer
/// bills compute against a monthly allowance of roughly 28 vCore-hours. Pinging
/// round the clock never lets the database pause, spends that allowance in
/// about two days, and leaves it unavailable for the rest of the month — the
/// opposite of what a keep-alive is for.
/// </para>
/// <para>
/// A league is played in bursts around fixtures, so the windows cover the hours
/// anyone is actually trading and the database sleeps the rest of the week.
/// Widening a window for a midweek fixture is a config change, not a deploy.
/// </para>
/// </summary>
public sealed class DatabaseKeepAliveOptions
{
    public const string SectionName = "DatabaseKeepAlive";

    /// <summary>
    /// Off unless a deployment says otherwise. On a paused-by-default database
    /// the safe failure is a cold start, not a spent allowance.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// The zone the windows are written in. Kick-off times are local, so a
    /// window meaning "Saturday afternoon" has to survive the clocks changing.
    /// </summary>
    public string TimeZone { get; set; } = "Europe/London";

    /// <summary>
    /// Smallest gap between probes while inside a window. The database pauses
    /// after an hour idle, so half that keeps one failed probe from mattering.
    /// </summary>
    public int MinimumMinutesBetweenProbes { get; set; } = 30;

    public IList<KeepAliveWindow> Windows { get; } = [];

    /// <summary>
    /// Whether <paramref name="utcNow"/> falls inside any window, judged in the
    /// configured zone.
    /// </summary>
    public bool IsInsideWindow(DateTimeOffset utcNow)
    {
        if (Windows.Count == 0)
        {
            return false;
        }

        var local = TimeZoneInfo.ConvertTime(utcNow, ResolveTimeZone());
        var day = local.DayOfWeek;
        var time = TimeOnly.FromTimeSpan(local.TimeOfDay);

        return Windows.Any(window => window.Contains(day, time));
    }

    /// <summary>
    /// Falls back to UTC rather than throwing. A mistyped zone should cost the
    /// schedule its accuracy, not take the whole application down at startup.
    /// </summary>
    private TimeZoneInfo ResolveTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(TimeZone);
        }
        catch (Exception exception) when (exception is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            return TimeZoneInfo.Utc;
        }
    }
}

public sealed class KeepAliveWindow
{
    /// <summary>Days the window applies to, as names: "Saturday", "Sunday".</summary>
    public IList<DayOfWeek> Days { get; } = [];

    /// <summary>Local start, "HH:mm".</summary>
    public string Start { get; set; } = "00:00";

    /// <summary>
    /// Local end, "HH:mm", exclusive. "24:00" is accepted for a window running
    /// to midnight, which <see cref="TimeOnly"/> cannot represent as an end.
    /// </summary>
    public string End { get; set; } = "00:00";

    internal bool Contains(DayOfWeek day, TimeOnly time)
    {
        if (!Days.Contains(day))
        {
            return false;
        }

        var start = ParseMinutes(Start);
        var end = ParseMinutes(End);

        // A time that would not parse shuts the window. Treating it as zero
        // would instead open the window earlier than anyone wrote down, so the
        // typo would show up as a bill rather than as a window that never runs.
        if (start < 0 || end < 0)
        {
            return false;
        }

        var minutes = (time.Hour * 60) + time.Minute;

        return minutes >= start && minutes < end;
    }

    /// <summary>
    /// Minutes past local midnight. Parsed rather than bound as a TimeOnly so
    /// that a typo yields a window that never opens, instead of an exception
    /// thrown from inside a background service on a timer tick.
    /// </summary>
    private static int ParseMinutes(string value)
    {
        var parts = value.Split(':');

        if (parts.Length != 2
            || !int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var hours)
            || !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var minutes)
            || hours > 24
            || minutes > 59)
        {
            return -1;
        }

        return (hours * 60) + minutes;
    }
}
