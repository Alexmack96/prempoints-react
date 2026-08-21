using Api.Domain.Entities;
using Api.Infrastructure.Paging;

namespace Api.Features.Teams.GetTeams;

/// <summary>
/// What <c>GET /teams</c> may be sorted by. Both keys are backed by an index —
/// TeamName by its unique index, and Id (the tiebreaker) by the primary key.
/// </summary>
public static class TeamSort
{
    public static readonly SortMap<TeamEntity> Map = new(
        defaultKey: "teamName",
        keys: new Dictionary<string, System.Linq.Expressions.Expression<Func<TeamEntity, object?>>>(StringComparer.OrdinalIgnoreCase)
        {
            ["teamName"] = team => team.TeamName,
            ["createdAt"] = team => team.CreatedAtUtc,
        });
}
