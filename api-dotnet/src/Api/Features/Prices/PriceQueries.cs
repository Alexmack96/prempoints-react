using Api.Domain.Entities;
using Api.Features.SeasonPeriods;
using Api.Features.Teams;
using Api.Infrastructure.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace Api.Features.Prices;

public static class PriceQueries
{
    public static async Task<PriceEntity?> GetByTeamNameAsync(PremPointsDbContext context, string teamName, DateOnly asAtDate = default, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(teamName);

        var team = await TeamQueries.GetByTeamNameAsync(context, teamName, ct)
            ?? throw new InvalidOperationException($"Can't find team with name: {teamName}");

        var seasonPeriod = await SeasonPeriodQueries.GetCurrent(context, asAtDate, ct)
            ?? throw new InvalidOperationException($"Can't find period including date: {asAtDate}");

        return await context.Prices.SingleOrDefaultAsync(t => t.TeamId == team.Id && seasonPeriod.Id == t.SeasonPeriodId, ct);
    }
}
