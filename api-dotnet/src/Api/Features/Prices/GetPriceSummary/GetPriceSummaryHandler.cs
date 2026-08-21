using Api.Infrastructure;
using Api.Infrastructure.EntityFramework;
using Api.Infrastructure.Paging;
using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static Api.Features.Prices.GetPriceSummary.GetPriceSummary;

namespace Api.Features.Prices.GetPriceSummary;

public class GetPriceSummaryHandler(PremPointsDbContext context, TimeProvider clock)
    : IRequestHandler<Query, Result<PagedResponse<TeamPriceSummaryDto>>>
{
    public async Task<Result<PagedResponse<TeamPriceSummaryDto>>> Handle(
        Query query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var asAtDate = query.AsAtDate ?? clock.UtcToday();

        var teams = await context.Teams
            .AsNoTracking()
            .Select(team => new { team.Id, team.TeamName })
            .ToListAsync(cancellationToken);

        // Two prices per club is what the movement arrow needs, and SQL Server
        // wants a window function to express "top 2 per group". At twenty clubs
        // and a price a gameweek this is a few hundred rows, so it is grouped in
        // memory instead — revisit if a club ever gets priced intraday.
        var priceHistory = await context.Prices
            .AsNoTracking()
            .Where(price => price.ValueDate <= asAtDate)
            .OrderByDescending(price => price.ValueDate)
            .Select(price => new
            {
                price.TeamId,
                price.Bid,
                price.Ask,
                price.Mid,
                price.ValueDate,
                price.PriceType,
            })
            .ToListAsync(cancellationToken);

        var byTeam = priceHistory
            .GroupBy(price => price.TeamId)
            .ToDictionary(group => group.Key, group => group.Take(2).ToList());

        var summaries = teams
            .Select(team =>
            {
                byTeam.TryGetValue(team.Id, out var prices);

                var latest = prices?.ElementAtOrDefault(0);
                var previous = prices?.ElementAtOrDefault(1);

                return new TeamPriceSummaryDto
                {
                    TeamId = team.Id,
                    TeamName = team.TeamName,
                    Bid = latest?.Bid,
                    Ask = latest?.Ask,
                    Mid = latest?.Mid,
                    ValueDate = latest?.ValueDate,
                    PriceType = latest?.PriceType,
                    PreviousMid = previous?.Mid,
                    Movement = Compare(latest?.Mid, previous?.Mid),
                };
            })
            // Highest price first, then by name so clubs on the same mid keep a
            // stable order between requests rather than shuffling.
            .OrderByDescending(summary => summary.Mid ?? decimal.MinValue)
            .ThenBy(summary => summary.TeamName, StringComparer.Ordinal)
            .ToList();

        var page = summaries
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToList();

        return PagedResponse<TeamPriceSummaryDto>.Create(page, query.Page, query.PageSize, summaries.Count);
    }

    private static PriceMovement Compare(decimal? latest, decimal? previous)
    {
        if (latest is null || previous is null)
        {
            return PriceMovement.Unknown;
        }

        return latest.Value.CompareTo(previous.Value) switch
        {
            > 0 => PriceMovement.Up,
            < 0 => PriceMovement.Down,
            _ => PriceMovement.Level,
        };
    }
}
