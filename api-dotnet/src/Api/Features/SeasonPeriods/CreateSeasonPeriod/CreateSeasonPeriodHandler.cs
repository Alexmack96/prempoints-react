using Api.Domain.Entities;
using Api.Infrastructure.EntityFramework;
using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static Api.Features.SeasonPeriods.CreateSeasonPeriod.CreateSeasonPeriod;

namespace Api.Features.SeasonPeriods.CreateSeasonPeriod;

public class CreateSeasonPeriodHandler(PremPointsDbContext context) : IRequestHandler<Command, Result<SeasonPeriodDto>>
{
    public async Task<Result<SeasonPeriodDto>> Handle(Command command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var currentSeason = await context.Seasons.SingleAsync(s => s.StartYear == command.SeasonStartYear, cancellationToken);

        var entity = new SeasonPeriodEntity { SeasonId = currentSeason.Id, PeriodStartDate = command.PeriodStartDate, PeriodEndDate = command.PeriodEndDate, GameweekNumber = command.GameweekNumber };

        context.Add(entity);

        await context.SaveChangesAsync(cancellationToken);

        return entity.ToDto();
    }
}
