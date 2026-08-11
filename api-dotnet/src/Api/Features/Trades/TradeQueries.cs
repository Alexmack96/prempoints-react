using Api.Domain.Entities;
using Api.Infrastructure.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace Api.Features.Trades;

public static class TradeQueries
{
    public static async Task<List<TradeEntity>> GetActiveBySeasonIdAsync(
        PremPointsDbContext context,
        Guid seasonId,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        return await context.Trades
            .AsNoTracking()
            .Where(t => t.SeasonPeriod.SeasonId == seasonId)

            // OPTIONAL: Since you likely want to see *who* traded *what*
            // you probably want to eager load the related data too:
            //.Include(t => t.Team)
            //.Include(t => t.User)
            //.Include(t => t.Price)
            //.Include(t => t.SeasonPeriod)
            .ToListAsync(ct);
    }
}