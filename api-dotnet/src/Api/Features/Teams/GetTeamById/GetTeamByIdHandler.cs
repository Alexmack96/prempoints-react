using Api.Infrastructure.EntityFramework;
using Ardalis.Result;
using MediatR;
using static Api.Features.Teams.GetTeamById.GetTeamById;

namespace Api.Features.Teams.GetTeamById;

public class GetTeamByIdHandler(ILogger<GetTeamByIdHandler> logger, PremPointsDbContext context)
    : IRequestHandler<Query, Result<TeamDto>>
{
    public async Task<Result<TeamDto>> Handle(Query query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var team = await TeamQueries.GetByIdAsync(context, query.Id, cancellationToken);
        if (team is null)
        {
            logger.LogWarning("Unable to find team with id: {TeamId}", query.Id);
            return Result.NotFound();
        }

        return team.ToDto();
    }
}
