using Api.Domain.Entities;
using Api.Infrastructure.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace Api.Features.Teams;

public static class TeamQueries
{
    public static async Task<TeamEntity?> GetByTeamNameAsync(PremPointsDbContext context, string teamName, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(teamName);
        ArgumentNullException.ThrowIfNull(context);
        return await context.Teams.SingleOrDefaultAsync(t => t.TeamName == teamName, ct);
    }
    public static async Task<List<TeamEntity>> GetActiveBySeasonIdAsync(
        PremPointsDbContext context,
        Guid seasonId,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        return await context.TeamSeasons
            .Where(ts => ts.SeasonId == seasonId)
            .Select(ts => ts.Team)
            .ToListAsync(ct);
    }
}