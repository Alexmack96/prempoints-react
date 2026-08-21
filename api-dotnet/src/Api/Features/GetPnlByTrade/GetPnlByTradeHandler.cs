using Api.Domain.Entities;
using Api.Features.GetPnlDetails;
using Api.Features.Seasons;
using Api.Features.Trades;
using Api.Features.Users;
using Api.Infrastructure;
using Api.Infrastructure.EntityFramework;
using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static Api.Features.GetPnlByTrade.GetPnlByTrade;

namespace Api.Features.GetPnlByTrade;

public class GetPnlByTradeHandler(ILogger<GetPnlByTradeHandler> logger, PremPointsDbContext context, TimeProvider clock) : IRequestHandler<Query, Result<List<PnlByTrade>>>
{
    public async Task<Result<List<PnlByTrade>>> Handle(Query query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.Username is null)
        {
            //Get all trades for this season
        }
        var userEntity = await UserQueries.GetByUsernameAsync(context, query.Username!, cancellationToken);
        if (userEntity is null)
        {
            logger.LogWarning("Unable to find user with name: {UserName}", query.Username);
            return Result.NotFound();
        }
        var effectiveDate = query.AsAtDate ?? clock.UtcToday();
        var requestedSeason = await SeasonQueries.GetByDateAsync(context, effectiveDate, cancellationToken);
        if (requestedSeason is null)
            return Result.NotFound($"AsAtDate {query.AsAtDate} does not sit in a valid season period.");

        return await context.Trades
            .AsNoTracking()
            .Where(t => t.SeasonPeriod.SeasonId == requestedSeason.Id)
            .Include(t => t.Team)
            .Include(t => t.User)
            .Include(t => t.Price)
            .Include(t => t.SeasonPeriod)
            .Select(trade => new PnlByTrade
            {
                TradeId = trade.Id,
                Username = trade.User.Username,
                TeamName = trade.Team.TeamName,
                TimezoneIana = trade.TimezoneIana,
                Exposure = trade.Exposure,
                GameweekNumber = trade.SeasonPeriod.GameweekNumber,
                PnlValue = 10,
                TradeDateUtc = trade.TradeDateUtc,
                TradeMultiplier = CalculateMultiplier(trade),
                IndexPriceProvisional = GetLatestPrice(effectiveDate, context),
                IndexPriceFinal = GetLatestPrice(effectiveDate, context),
                SeasonStartYear = requestedSeason.StartYear,
                TradePrice = trade.Price.Price,
            })
            .ToListAsync(cancellationToken);
    }

    private decimal GetLatestPrice(DateOnly effectiveDate, PremPointsDbContext context)
    {
        //If effectivedate is a finalDate, just lookup the price

        throw new NotImplementedException();
    }

    private static int CalculateMultiplier(TradeEntity tradeEntity)
    {
        switch (tradeEntity.TradeType)
        {
            case TradeType.Standard:
                return 1;
            case TradeType.Joker:
                return 2;
            case TradeType.ManagerOfTheMonth:
                return 2;
            default:
                string validEnums = string.Join(", ", Enum.GetNames<TradeType>());
                throw new ArgumentException($"Invalid Trade Type: '{tradeEntity.TradeType}'. Valid enums are: {validEnums}");
        }
    }
}