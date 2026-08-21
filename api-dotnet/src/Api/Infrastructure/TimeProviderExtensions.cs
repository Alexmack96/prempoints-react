namespace Api.Infrastructure;

public static class TimeProviderExtensions
{
    /// <summary>
    /// Today's date in UTC, as the <see cref="DateOnly"/> the season and period
    /// lookups are keyed on.
    /// <para>
    /// Every "as at" default in the feature handlers goes through here so there
    /// is one place that turns an instant into a date, and so pinning the clock
    /// in a test pins the date too.
    /// </para>
    /// </summary>
    public static DateOnly UtcToday(this TimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        return DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
    }
}
