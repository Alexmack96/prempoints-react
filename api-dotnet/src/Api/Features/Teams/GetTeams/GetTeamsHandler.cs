using Api.Features.Seasons;
using Api.Infrastructure.EntityFramework;
using Api.Infrastructure.Paging;
using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static Api.Features.Teams.GetTeams.GetTeams;

namespace Api.Features.Teams.GetTeams;

public class GetTeamsHandler(PremPointsDbContext context, TimeProvider clock)
    : IRequestHandler<Query, Result<PagedResponse<TeamDto>>>
{
    public async Task<Result<PagedResponse<TeamDto>>> Handle(Query query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        Guid? activeInSeasonId = null;

        if (query.ActiveOn is { } activeOn)
        {
            var season = await SeasonQueries.GetByDateAsync(context, activeOn, cancellationToken);

            // No season covers that date, so no team is active on it. That is an
            // empty page, not a 404: the caller asked a well-formed question
            // about a collection and the honest answer is "none".
            if (season is null)
            {
                return PagedResponse<TeamDto>.Empty(query.Page, query.PageSize);
            }

            activeInSeasonId = season.Id;
        }

        var filtered = TeamQueries.Filter(context, activeInSeasonId, query.Name);

        var totalCount = await filtered.CountAsync(cancellationToken);

        var items = await TeamSort.Apply(filtered, query.Sort)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(t => new TeamDto { Id = t.Id, TeamName = t.TeamName })
            .ToListAsync(cancellationToken);

        return PagedResponse<TeamDto>.Create(items, query.Page, query.PageSize, totalCount);
    }
}
