using Api.Domain.Entities;
using Api.Features.Users;
using Api.Infrastructure.EntityFramework;
using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static Api.Features.Trades.CreateTrades.CreateTrades;

namespace Api.Features.Trades.CreateTrades;

public class CreateTradesHandler(PremPointsDbContext context) : IRequestHandler<Command, Result<List<TradeDto>>>
{
    public async Task<Result<List<TradeDto>>> Handle(Command command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var user = await UserQueries.GetByUsernameAsync(context, command.Username, cancellationToken);
        if (user is null)
            return Result.NotFound($"User '{command.Username}' not found.");

        var valueDate = DateOnly.FromDateTime(command.TradeDateUtc);
        var requestedTeamNames = command.ExposuresByTeam.Keys.ToList();

        var teams = await context.Teams
            .Where(t => requestedTeamNames.Contains(t.TeamName))
            .ToDictionaryAsync(t => t.TeamName.ToUpperInvariant(), t => t, cancellationToken);

        var prices = await context.Prices
            .Where(p => requestedTeamNames.Contains(p.Team.TeamName) && p.ValueDate == valueDate)
            .Include(p => p.Team)
            .ToListAsync(cancellationToken);

        var pricesByTeamName = prices.ToDictionary(p => p.Team.TeamName.ToUpperInvariant(), p => p);

        // 5. Idempotency & Persistence
        var existingTrades = await context.Trades
            .Where(t => t.UserId == user.Id && t.TradeDateUtc.Date == command.TradeDateUtc.Date)
            .ToListAsync(cancellationToken);

        List<TradeEntity> resultEntities = [];
        HashSet<Guid> keptTradeIds = []; // Track IDs of existing trades we touch

        foreach (var exposureByTeam in command.ExposuresByTeam)
        {
            var teamName = exposureByTeam.Key;
            var exposure = exposureByTeam.Value;

            var team = teams[teamName.ToUpperInvariant()];
            var price = pricesByTeamName[teamName.ToUpperInvariant()];

            var existingTrade = existingTrades.FirstOrDefault(t => t.TeamId == team.Id);
            if (existingTrade is not null)
            {
                // UPDATE
                existingTrade.Exposure = exposure;
                existingTrade.PriceId = price.Id;
                existingTrade.Price = price;
                existingTrade.TimezoneIana = command.TimezoneIana;
                // Track for return
                keptTradeIds.Add(existingTrade.Id);
                resultEntities.Add(existingTrade);
            }
            else
            {
                // INSERT
                var newTrade = new TradeEntity
                {
                    Id = Guid.CreateVersion7(),
                    //FKs & navigation
                    UserId = user.Id,
                    TeamId = team.Id,
                    PriceId = price.Id,
                    SeasonPeriodId = price.SeasonPeriodId,
                    Team = team,
                    Price = price,
                    //Other fields
                    TradeDateUtc = command.TradeDateUtc,
                    Exposure = exposure,
                    TradeType = command.TradeType,
                    TimezoneIana = command.TimezoneIana
                };

                context.Trades.Add(newTrade);
                resultEntities.Add(newTrade);
            }
        }

        //Delete remaining orphaned trades
        var tradesToRemove = existingTrades.Where(t => !keptTradeIds.Contains(t.Id));
        context.Trades.RemoveRange(tradesToRemove);

        await context.SaveChangesAsync(cancellationToken);

        return resultEntities.ToDtos();
    }
}