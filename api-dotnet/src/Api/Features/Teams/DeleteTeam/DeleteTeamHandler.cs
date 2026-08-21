using Api.Infrastructure.EntityFramework;
using Ardalis.Result;
using MediatR;
using static Api.Features.Teams.DeleteTeam.DeleteTeam;

namespace Api.Features.Teams.DeleteTeam;

public class DeleteTeamHandler(ILogger<DeleteTeamHandler> logger, PremPointsDbContext context)
    : IRequestHandler<Command, Result>
{
    public async Task<Result> Handle(Command command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var team = await TeamQueries.GetByIdAsync(context, command.Id, cancellationToken);
        if (team is null)
        {
            logger.LogWarning("Unable to delete team with id: {TeamId}", command.Id);
            return Result.NotFound();
        }

        // Asked before the delete rather than caught after it: every foreign key
        // into Teams is Restrict, so letting the database refuse would surface
        // as an opaque 500 that never says which relationship blocked it.
        var references = await TeamQueries.CountReferencesAsync(context, command.Id, cancellationToken);
        if (references.Any)
        {
            return Result.Conflict(
                $"Team '{team.TeamName}' cannot be deleted while it has {references.Describe()}.");
        }

        context.Teams.Remove(team);
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
