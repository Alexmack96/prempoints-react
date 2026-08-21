using Api.Domain.Entities;
using Api.Features.SeasonPeriods;
using Api.Features.Teams;
using Api.Infrastructure.EntityFramework;
using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static Api.Features.Prices.CreatePrice.CreatePrice;

namespace Api.Features.Prices.CreatePrice;

public class CreatePriceHandler(PremPointsDbContext context, TimeProvider clock) : IRequestHandler<Command, Result<PriceDto>>
{
    public async Task<Result<PriceDto>> Handle(Command command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var team = await TeamQueries.GetByTeamNameAsync(context, command.TeamName, cancellationToken);
        if (team is null)
            return Result.NotFound($"No valid team with name: {command.TeamName}");

        var currentSeasonPeriod = await SeasonPeriodQueries.GetCurrent(context, command.ValueDate, cancellationToken);
        if (currentSeasonPeriod is null)
            return Result.NotFound($"ValueDate {command.ValueDate} does not sit in a valid season period.");

        //We want upsert behaviour if someone sends up a new price for an existing value date
        var existingPrice = await context.Prices
            .SingleOrDefaultAsync(pr => pr.ValueDate == command.ValueDate && pr.TeamId == team.Id, cancellationToken);
        if (existingPrice is not null)
        {
            existingPrice.Price = command.Price;
            existingPrice.SeasonPeriodId = currentSeasonPeriod.Id;
        }
        else
        {
            var newPriceEntity = new PriceEntity
            {
                Id = Guid.CreateVersion7(clock.GetUtcNow()),
                TeamId = team.Id,
                SeasonPeriodId = currentSeasonPeriod.Id,
                Price = command.Price,
                ValueDate = command.ValueDate,
                PriceType = CalculatePriceType(currentSeasonPeriod.PeriodEndDate, command.ValueDate)
            };

            context.Prices.Add(newPriceEntity);
            existingPrice = newPriceEntity;
        }
        await context.SaveChangesAsync(cancellationToken);
        return existingPrice.ToDto();
    }

    private static PriceType CalculatePriceType(DateOnly periodEndDate, DateOnly valueDate)
    {
        return valueDate == periodEndDate.AddDays(1)
            ? PriceType.Final
            : PriceType.Provisional;
    }
}
