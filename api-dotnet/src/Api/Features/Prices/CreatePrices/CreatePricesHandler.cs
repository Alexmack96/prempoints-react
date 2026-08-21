using Api.Domain.Entities;
using Api.Features.SeasonPeriods;
using Api.Infrastructure.EntityFramework;
using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static Api.Features.Prices.CreatePrices.CreatePrices;

namespace Api.Features.Prices.CreatePrices;

public class CreatePricesHandler(PremPointsDbContext context, TimeProvider clock)
    : IRequestHandler<Command, Result<List<PriceDto>>>
{
    public async Task<Result<List<PriceDto>>> Handle(Command command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var seasonPeriod = await SeasonPeriodQueries.GetCurrent(context, command.ValueDate, cancellationToken);
        if (seasonPeriod is null)
        {
            return Result.NotFound($"ValueDate {command.ValueDate:yyyy-MM-dd} does not sit in a valid season period.");
        }

        var requestedNames = command.Prices.Select(price => price.TeamName).ToList();

        var teams = await context.Teams
            .Where(team => requestedNames.Contains(team.TeamName))
            .ToDictionaryAsync(team => team.TeamName, StringComparer.OrdinalIgnoreCase, cancellationToken);

        // Checked before anything is written. A partial price board is worse
        // than none: the clubs that made it through would be tradeable and the
        // rest silently would not, which nobody would notice until a player
        // tried to back the missing one.
        var unknown = requestedNames
            .Where(name => !teams.ContainsKey(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (unknown.Count > 0)
        {
            return Result.NotFound($"No team with name: {string.Join(", ", unknown)}.");
        }

        var teamIds = teams.Values.Select(team => team.Id).ToList();

        var existing = await context.Prices
            .Where(price => price.ValueDate == command.ValueDate && teamIds.Contains(price.TeamId))
            .ToDictionaryAsync(price => price.TeamId, cancellationToken);

        var priceType = CalculatePriceType(seasonPeriod.PeriodEndDate, command.ValueDate);
        var results = new List<PriceEntity>();

        foreach (var spread in command.Prices)
        {
            var team = teams[spread.TeamName];

            // Upsert, matching the single-price endpoint: re-running a load with
            // corrected numbers should fix the board, not fail on the unique
            // index over team and value date.
            if (existing.TryGetValue(team.Id, out var price))
            {
                price.Bid = spread.Bid;
                price.Ask = spread.Ask;
                price.SeasonPeriodId = seasonPeriod.Id;
                price.PriceType = priceType;
            }
            else
            {
                price = new PriceEntity
                {
                    Id = Guid.CreateVersion7(clock.GetUtcNow()),
                    TeamId = team.Id,
                    SeasonPeriodId = seasonPeriod.Id,
                    Bid = spread.Bid,
                    Ask = spread.Ask,
                    ValueDate = command.ValueDate,
                    PriceType = priceType,
                };

                context.Prices.Add(price);
            }

            results.Add(price);
        }

        await context.SaveChangesAsync(cancellationToken);

        // No reload needed for Mid. A stored computed column is
        // ValueGeneratedOnAddOrUpdate, so EF reads it back through the OUTPUT
        // clause of the same statement that wrote the spread.
        return results.ToDtos();
    }

    private static PriceType CalculatePriceType(DateOnly periodEndDate, DateOnly valueDate) =>
        valueDate == periodEndDate.AddDays(1) ? PriceType.Final : PriceType.Provisional;
}
