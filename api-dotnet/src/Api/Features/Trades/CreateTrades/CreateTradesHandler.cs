using Api.Domain.Entities;
using Api.Features.SeasonPeriods;
using Api.Features.Users;
using Api.Infrastructure.EntityFramework;
using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static Api.Features.Trades.CreateTrades.CreateTrades;

namespace Api.Features.Trades.CreateTrades;

public class CreateTradesHandler(PremPointsDbContext context, TimeProvider clock) : IRequestHandler<Command, Result<List<TradeDto>>>
{
    public async Task<Result<List<TradeDto>>> Handle(Command command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var user = await UserQueries.GetByWorkOSIdAsync(context, command.WorkOsUserId, cancellationToken);
        if (user is null)
        {
            // Authenticated with WorkOS but not a player here yet.
            return Result.NotFound("You are signed in but do not have a PremPoints account yet.");
        }

        var valueDate = DateOnly.FromDateTime(command.TradeDateUtc);

        var seasonPeriod = await SeasonPeriodQueries.GetCurrent(context, valueDate, cancellationToken);
        if (seasonPeriod is null)
        {
            return Result.NotFound($"{valueDate:yyyy-MM-dd} does not sit in a valid season period.");
        }

        // Checked before anything is written, so a rejected joker leaves no
        // half-applied submission behind.
        var jokerRefusal = await CheckJokerAllowanceAsync(command, user.Id, seasonPeriod.SeasonId, cancellationToken);
        if (jokerRefusal is not null)
        {
            return jokerRefusal;
        }

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

            // Looked up rather than indexed. Both of these were bare indexer
            // reads, so a team the caller invented — or, far more likely, a real
            // team with no price loaded for that date — threw
            // KeyNotFoundException and reached the client as a 500 saying
            // nothing. Missing prices are an ordinary operational state at the
            // start of a gameweek, not a server fault.
            if (!teams.TryGetValue(teamName.ToUpperInvariant(), out var team))
            {
                return Result.NotFound($"Team '{teamName}' does not exist.");
            }

            if (!pricesByTeamName.TryGetValue(teamName.ToUpperInvariant(), out var price))
            {
                return Result.NotFound(
                    $"No price for '{teamName}' on {valueDate:yyyy-MM-dd}. Prices must be loaded before trades can be placed.");
            }

            var existingTrade = existingTrades.FirstOrDefault(t => t.TeamId == team.Id);
            if (existingTrade is not null)
            {
                // UPDATE
                existingTrade.Exposure = exposure;
                existingTrade.PriceId = price.Id;
                existingTrade.Price = price;
                existingTrade.TimezoneIana = command.TimezoneIana;
                // Was missing, so re-submitting with the joker on kept the
                // original Standard and the multiplier silently never applied.
                existingTrade.TradeType = command.TradeType;
                // Track for return
                keptTradeIds.Add(existingTrade.Id);
                resultEntities.Add(existingTrade);
            }
            else
            {
                // INSERT
                var newTrade = new TradeEntity
                {
                    Id = Guid.CreateVersion7(clock.GetUtcNow()),
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
    /// <summary>
    /// Refuses a second joker in the same season and calendar year, or null if
    /// the joker is allowed.
    /// <para>
    /// The allowance is one joker per calendar year <em>within a season</em>.
    /// A season straddles New Year, so that works out at two per season — one
    /// before Christmas and one after. Scoping it to the season as well as the
    /// year is what lets January 2026 and November 2026 both be jokers: same
    /// calendar year, but 2025/26 and 2026/27 respectively.
    /// </para>
    /// </summary>
    private async Task<Result<List<TradeDto>>?> CheckJokerAllowanceAsync(
        Command command,
        Guid userId,
        Guid seasonId,
        CancellationToken cancellationToken)
    {
        if (command.TradeType != TradeType.Joker)
        {
            return null;
        }

        var blockedBy = await JokerQueries.FindBlockingJokerAsync(
            context, userId, seasonId, command.TradeDateUtc, cancellationToken);

        if (blockedBy is null)
        {
            return null;
        }

        return Result.Conflict(
            $"You have already played your joker for {command.TradeDateUtc.Year} this season, " +
            $"on {blockedBy:yyyy-MM-dd}. You get one per calendar year within a season.");
    }

}