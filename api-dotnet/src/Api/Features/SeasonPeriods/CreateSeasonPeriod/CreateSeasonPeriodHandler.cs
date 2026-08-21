using Api.Domain.Entities;
using Api.Infrastructure.EntityFramework;
using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static Api.Features.SeasonPeriods.CreateSeasonPeriod.CreateSeasonPeriod;

namespace Api.Features.SeasonPeriods.CreateSeasonPeriod;

public class CreateSeasonPeriodHandler(PremPointsDbContext context, TimeProvider clock) : IRequestHandler<Command, Result<SeasonPeriodDto>>
{
    public async Task<Result<SeasonPeriodDto>> Handle(Command command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var currentSeason = await context.Seasons.SingleOrDefaultAsync(s => s.StartYear == command.SeasonStartYear, cancellationToken);
        if (currentSeason is null)
            return Result.NotFound("Season Not Found", $"No season starts in {command.SeasonStartYear}.");

        // Ids are ValueGenerated.Never for auditable entities (see
        // PremPointsDbContext.OnModelCreating), so the handler owns this.
        var entity = new SeasonPeriodEntity
        {
            Id = Guid.CreateVersion7(clock.GetUtcNow()),
            SeasonId = currentSeason.Id,
            PeriodStartDate = command.PeriodStartDate,
            PeriodEndDate = command.PeriodEndDate,
            GameweekNumber = command.GameweekNumber,
        };

        context.Add(entity);

        await context.SaveChangesAsync(cancellationToken);

        return entity.ToDto();
    }
}
