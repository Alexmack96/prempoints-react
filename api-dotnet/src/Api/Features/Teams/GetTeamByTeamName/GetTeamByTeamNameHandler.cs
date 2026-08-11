using Api.Infrastructure.EntityFramework;
using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static Api.Features.Teams.GetTeamByTeamName.GetTeamByTeamName;

namespace Api.Features.Teams.GetTeamByTeamName;

public class GetTeamByTeamNameHandler(ILogger<GetTeamByTeamNameHandler> logger, PremPointsDbContext context) : IRequestHandler<Query, Result<TeamDto>>
{
    public async Task<Result<TeamDto>> Handle(Query query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var teamEntity = await TeamQueries.GetByTeamNameAsync(context, query.TeamName, cancellationToken);
        if (teamEntity is null)
        {
            logger.LogWarning("Unable to find team with name: {TeamName}", query.TeamName);
            return Result.NotFound();
        }

        return teamEntity.ToDto();
    }
}
