using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Api.Infrastructure.EntityFramework;

/// <summary>
/// Holds the Azure SQL serverless database open during the hours anyone is
/// likely to be trading, and lets it pause the rest of the week.
/// <para>
/// The database auto-pauses after an hour idle, and the first connection after
/// that waits 30 to 60 seconds for a resume while returning transient failures.
/// Probing round the clock would avoid that, and on the free offer would also
/// spend a month's compute allowance in about two days — so the schedule in
/// <see cref="DatabaseKeepAliveOptions"/> decides when it is worth paying for.
/// </para>
/// <para>
/// This does nothing for the cold start at deploy time. Hosted services do not
/// begin until <c>RunAsync</c>, and the migration in Program.cs runs before it,
/// so a deploy onto a paused database still waits for the resume with no help
/// from here.
/// </para>
/// </summary>
public sealed class DatabaseKeepAlive(
    IServiceScopeFactory scopeFactory,
    IOptions<DatabaseKeepAliveOptions> options,
    TimeProvider timeProvider,
    ILogger<DatabaseKeepAlive> logger) : BackgroundService
{
    /// <summary>
    /// How often the schedule is consulted. Deliberately shorter than the gap
    /// between probes: a tick costs nothing when the window is shut, and it
    /// means the first probe lands within five minutes of a window opening
    /// rather than up to half an hour into it.
    /// </summary>
    private static readonly TimeSpan TickInterval = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        var minimumGap = TimeSpan.FromMinutes(settings.MinimumMinutesBetweenProbes);

        logger.LogInformation(
            "Database keep-alive scheduled across {Windows} window(s) in {TimeZone}.",
            settings.Windows.Count,
            settings.TimeZone);

        // TimeProvider rather than the ambient clock, so a test can advance time
        // instead of waiting for a window to come round.
        using var timer = new PeriodicTimer(TickInterval, timeProvider);

        var lastProbe = DateTimeOffset.MinValue;

        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
        {
            var now = timeProvider.GetUtcNow();

            if (!settings.IsInsideWindow(now) || now - lastProbe < minimumGap)
            {
                continue;
            }

            lastProbe = now;

            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var database = scope.ServiceProvider.GetRequiredService<PremPointsDbContext>();

                await database.Database
                    .ExecuteSqlRawAsync("SELECT 1", stoppingToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
#pragma warning disable CA1031 // Catching everything is the point, see below.
            catch (Exception ex)
#pragma warning restore CA1031
            {
                // An unhandled exception out of ExecuteAsync stops the whole
                // host by default, so a transient SQL blip in a probe whose
                // only job is to keep a connection warm would take the API
                // down with it. Log it and wait for the next tick.
                logger.LogWarning(ex, "Database keep-alive probe failed.");
            }
        }
    }
}
