using Microsoft.EntityFrameworkCore;

namespace Api.Infrastructure.EntityFramework;

/// <summary>
/// Holds the Azure SQL serverless database open by touching it on a timer.
/// <para>
/// The database auto-pauses after an hour idle, and the first connection after
/// that waits 30 to 60 seconds for a resume while returning transient failures.
/// A league that is quiet overnight would meet that wait every morning.
/// </para>
/// <para>
/// This does nothing for the cold start at deploy time. Hosted services do not
/// begin until <c>RunAsync</c>, and the migration in Program.cs runs before it,
/// so a deploy onto a paused database still waits for the resume with no help
/// from here. Only disabling auto-pause fixes that one.
/// </para>
/// </summary>
public sealed class DatabaseKeepAlive(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<DatabaseKeepAlive> logger) : BackgroundService
{
    /// <summary>
    /// Half the one hour auto-pause delay, so a single failed probe still
    /// leaves a second attempt before the idle window closes.
    /// </summary>
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // TimeProvider rather than the ambient clock, so a test can advance
        // time instead of waiting half an hour for the first tick.
        using var timer = new PeriodicTimer(Interval, timeProvider);

        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
        {
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
