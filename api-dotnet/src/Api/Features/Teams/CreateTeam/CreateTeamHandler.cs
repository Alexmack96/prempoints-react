using Api.Domain.Entities;
using Api.Features.Seasons;
using Api.Infrastructure.EntityFramework;
using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static Api.Features.Teams.CreateTeam.CreateTeam;

namespace Api.Features.Teams.CreateTeam;

public class CreateTeamHandler(PremPointsDbContext context) : IRequestHandler<Command, Result<TeamDto>>
{
    public async Task<Result<TeamDto>> Handle(Command command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var teamExists = await context.Teams.AnyAsync(t => t.TeamName == command.TeamName, cancellationToken);
        if (teamExists)
            return Result.Conflict("DuplicateName", $"Team '{command.TeamName}' already exists.");

        var season = await SeasonQueries.GetByDateAsync(context, DateOnly.FromDateTime(DateTime.UtcNow), cancellationToken);
        if (season is null)
            return Result.NotFound("Season Not Found", $"No valid season found at: '{DateTime.UtcNow}'.");

        var entity = new TeamEntity { Id = Guid.CreateVersion7(), TeamName = command.TeamName };

        var teamSeason = new TeamSeasonEntity { Id = Guid.CreateVersion7(), Team = entity, Season = season };

        context.Teams.Add(entity);
        context.TeamSeasons.Add(teamSeason);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Result.Conflict("DuplicateName", $"Team '{command.TeamName}' already exists.");
        }

        return entity.ToDto();
    }
}
