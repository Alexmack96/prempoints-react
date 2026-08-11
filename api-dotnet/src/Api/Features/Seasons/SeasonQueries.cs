using Api.Domain.Entities;
using Api.Infrastructure.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace Api.Features.Seasons;

public static class SeasonQueries
{
    public static async Task<SeasonEntity?> GetByDateAsync(PremPointsDbContext context, DateOnly asAtDate, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        return await context.SeasonPeriods
            .Where(sp => sp.PeriodStartDate <= asAtDate && asAtDate <= sp.PeriodEndDate)
            .Include(sp => sp.Season)
            .Select(sp => sp.Season)
            .SingleOrDefaultAsync(ct);
    }
}
