using Api.Features.Seasons;
using Api.Infrastructure;
using Api.Infrastructure.EntityFramework;
using Api.Infrastructure.Paging;
using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static Api.Features.Leaderboard.GetLeaderboard.GetLeaderboard;

namespace Api.Features.Leaderboard.GetLeaderboard;

public class GetLeaderboardHandler(PremPointsDbContext context, TimeProvider clock)
    : IRequestHandler<Query, Result<PagedResponse<LeaderboardRowDto>>>
{
    /// <summary>
    /// What a trade is currently worth.
    /// <para>
    /// Zero, deliberately. Marking a trade means comparing the price it was
    /// struck at against the settled price for its gameweek, and settlement is
    /// not built yet — <c>GetPnlByTradeHandler.GetLatestPrice</c> is still a
    /// <c>NotImplementedException</c>. Zero is the honest answer until it is,
    /// and <see cref="LeaderboardRowDto.PnlIsSettled"/> tells the client that
    /// is what it is looking at.
    /// </para>
    /// <para>
    /// It lives here, alone, so that landing settlement is a change to this one
    /// expression rather than a rewrite of the query below.
    /// </para>
    /// </summary>
    private const decimal UnsettledPnl = 0m;

    public async Task<Result<PagedResponse<LeaderboardRowDto>>> Handle(
        Query query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var asAtDate = query.AsAtDate ?? clock.UtcToday();

        var season = await SeasonQueries.GetByDateAsync(context, asAtDate, cancellationToken);
        if (season is null)
        {
            return Result.NotFound($"No season found covering the date {asAtDate}.");
        }

        // Enrolments, not users: being in the season is what puts a player on
        // the board, and a user row with no enrolment is someone who signed up
        // and never joined. The trade count is a correlated subquery rather
        // than an Include, so it comes back as a COUNT rather than as every
        // trade row for every player.
        var standings = await context.UserSeasons
            .AsNoTracking()
            .Where(enrolment => enrolment.SeasonId == season.Id)
            .Select(enrolment => new
            {
                enrolment.UserId,
                enrolment.User.Username,
                enrolment.User.FirstName,
                enrolment.User.LastName,
                TradesPlaced = context.Trades.Count(trade =>
                    trade.UserId == enrolment.UserId &&
                    trade.SeasonPeriod.SeasonId == enrolment.SeasonId &&
                    trade.SeasonPeriod.PeriodStartDate <= asAtDate),
            })
            .ToListAsync(cancellationToken);

        // Best first, then the busiest, then by name — so two players on the
        // same score keep the same order between requests instead of shuffling
        // whenever SQL Server feels like returning them differently.
        var ordered = standings
            .Select(row => new
            {
                row.UserId,
                row.Username,
                row.FirstName,
                row.LastName,
                row.TradesPlaced,
                Pnl = UnsettledPnl,
            })
            .OrderByDescending(row => row.Pnl)
            .ThenByDescending(row => row.TradesPlaced)
            .ThenBy(row => row.Username, StringComparer.Ordinal)
            .ToList();

        // Equal scores share a rank, which on day one means the whole league is
        // joint first. Ranking before paging, because a rank that restarted at
        // 1 on page two would be a different number for the same player.
        var ranks = new int[ordered.Count];
        for (var index = 0; index < ordered.Count; index++)
        {
            ranks[index] = index > 0 && ordered[index].Pnl == ordered[index - 1].Pnl
                ? ranks[index - 1]
                : index + 1;
        }

        var page = ordered
            .Select((row, index) => new LeaderboardRowDto
            {
                Rank = ranks[index],
                UserId = row.UserId,
                Username = row.Username,
                FirstName = row.FirstName,
                LastName = row.LastName,
                Pnl = row.Pnl,
                TradesPlaced = row.TradesPlaced,
                PnlIsSettled = false,
            })
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToList();

        return PagedResponse<LeaderboardRowDto>.Create(page, query.Page, query.PageSize, ordered.Count);
    }
}
