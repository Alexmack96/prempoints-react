using Api.Features.SeasonPeriods;
using Api.Features.Users;
using Api.Infrastructure.EntityFramework;
using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static Api.Features.Trades.GetJokerAllowance.GetJokerAllowance;

namespace Api.Features.Trades.GetJokerAllowance;

public class GetJokerAllowanceHandler(PremPointsDbContext context, TimeProvider clock)
    : IRequestHandler<Query, Result<JokerAllowanceDto>>
{
    public async Task<Result<JokerAllowanceDto>> Handle(Query query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var user = await UserQueries.GetByWorkOSIdAsync(context, query.WorkOsUserId, cancellationToken);
        if (user is null)
        {
            return Result.NotFound("You are signed in but do not have a PremPoints account yet.");
        }

        var tradeDate = query.TradeDateUtc ?? clock.GetUtcNow().UtcDateTime;
        var valueDate = DateOnly.FromDateTime(tradeDate);

        var seasonPeriod = await SeasonPeriodQueries.GetCurrent(context, valueDate, cancellationToken);
        if (seasonPeriod is null)
        {
            return Result.NotFound($"{valueDate:yyyy-MM-dd} does not sit in a valid season period.");
        }

        var season = await context.Seasons
            .AsNoTracking()
            .SingleAsync(s => s.Id == seasonPeriod.SeasonId, cancellationToken);

        // The same query the write path uses, so the checkbox and the API can
        // never disagree about whether a joker is available.
        var blockedBy = await JokerQueries.FindBlockingJokerAsync(
            context, user.Id, season.Id, tradeDate, cancellationToken);

        var played = await JokerQueries.GetPlayedInSeasonAsync(
            context, user.Id, season.Id, cancellationToken);

        return new JokerAllowanceDto
        {
            SeasonId = season.Id,
            SeasonName = season.SeasonName,
            CalendarYear = tradeDate.Year,
            Available = blockedBy is null,
            BlockedByUtc = blockedBy,
            PlayedThisSeason = [.. played.Select(use => new JokerUseDto
            {
                CalendarYear = use.CalendarYear,
                PlayedOnUtc = use.PlayedOn,
            })],
        };
    }
}
