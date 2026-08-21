using Api.Infrastructure.EntityFramework;
using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static Api.Features.Teams.UpdateTeam.UpdateTeam;

namespace Api.Features.Teams.UpdateTeam;

public class UpdateTeamHandler(ILogger<UpdateTeamHandler> logger, PremPointsDbContext context)
    : IRequestHandler<Command, Result<TeamDto>>
{
    public async Task<Result<TeamDto>> Handle(Command command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var team = await TeamQueries.GetByIdAsync(context, command.Id, cancellationToken);
        if (team is null)
        {
            logger.LogWarning("Unable to update team with id: {TeamId}", command.Id);
            return Result.NotFound();
        }

        // exceptId, so renaming a team to the name it already has is a no-op
        // that succeeds rather than a 409 against itself.
        if (await TeamQueries.NameExistsAsync(context, command.TeamName, command.Id, cancellationToken))
        {
            return Result.Conflict($"Team '{command.TeamName}' already exists.");
        }

        team.TeamName = command.TeamName;

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Result.Conflict($"Team '{command.TeamName}' already exists.");
        }

        return team.ToDto();
    }
}
