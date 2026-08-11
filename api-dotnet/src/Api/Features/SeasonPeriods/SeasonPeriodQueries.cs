using Api.Domain.Entities;
using Api.Infrastructure.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace Api.Features.SeasonPeriods;

public static class SeasonPeriodQueries
{
    public static async Task<SeasonPeriodEntity?> GetCurrent(PremPointsDbContext context, DateOnly asAtDate, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        return await context.SeasonPeriods
            .Where(sp => sp.PeriodStartDate <= asAtDate && asAtDate <= sp.PeriodEndDate)
            .SingleOrDefaultAsync(ct);
    }
}
