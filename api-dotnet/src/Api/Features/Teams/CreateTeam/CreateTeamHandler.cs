using Api.Domain.Entities;
using Api.Features.Seasons;
using Api.Infrastructure;
using Api.Infrastructure.EntityFramework;
using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static Api.Features.Teams.CreateTeam.CreateTeam;

namespace Api.Features.Teams.CreateTeam;

public class CreateTeamHandler(PremPointsDbContext context, TimeProvider clock)
    : IRequestHandler<Command, Result<TeamDto>>
{
    public async Task<Result<TeamDto>> Handle(Command command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Checked up front so the common case gets a clear 409 rather than one
        // reverse-engineered from a database error. The catch below still has to
        // exist: two requests can pass this check concurrently, and only the
        // unique index can actually decide the race.
        if (await TeamQueries.NameExistsAsync(context, command.TeamName, ct: cancellationToken))
        {
            return Result.Conflict($"Team '{command.TeamName}' already exists.");
        }

        var today = clock.UtcToday();
        var season = await SeasonQueries.GetByDateAsync(context, today, cancellationToken);
        if (season is null)
        {
            return Result.NotFound($"No season is running on {today:O}, so a team cannot be enrolled.");
        }

        var team = new TeamEntity
        {
            Id = Guid.CreateVersion7(clock.GetUtcNow()),
            TeamName = command.TeamName,
        };

        var teamSeason = new TeamSeasonEntity
        {
            Id = Guid.CreateVersion7(clock.GetUtcNow()),
            Team = team,
            Season = season,
        };

        context.Teams.Add(team);
        context.TeamSeasons.Add(teamSeason);

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
