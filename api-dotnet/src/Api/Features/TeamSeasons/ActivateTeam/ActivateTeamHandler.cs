using Api.Domain.Entities;
using Api.Features.Seasons;
using Api.Infrastructure;
using Api.Infrastructure.EntityFramework;
using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static Api.Features.TeamSeasons.ActivateTeam.ActivateTeam;

namespace Api.Features.TeamSeasons.ActivateTeam;

public class ActivateTeamHandler(PremPointsDbContext context, TimeProvider clock) : IRequestHandler<Command, Result<TeamSeasonDto>>
{
    public async Task<Result<TeamSeasonDto>> Handle(Command command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var requestedTeam = await context.Teams.SingleAsync(u => u.TeamName == command.TeamName, cancellationToken);
        var effectiveDate = command.AsAtDate ?? clock.UtcToday();

        var requestedSeason = await SeasonQueries.GetByDateAsync(context, effectiveDate, cancellationToken);
        if (requestedSeason is null)
            return Result.NotFound("");

        var teamSeason = new TeamSeasonEntity { Id = Guid.CreateVersion7(clock.GetUtcNow()), Team = requestedTeam, Season = requestedSeason };

        context.TeamSeasons.Add(teamSeason);
        await context.SaveChangesAsync(cancellationToken);
        return teamSeason.ToDto();
    }
}
