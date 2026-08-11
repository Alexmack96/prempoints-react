using Api.Domain.Entities;
using Api.Features.Seasons;
using Api.Infrastructure.EntityFramework;
using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static Api.Features.TeamSeasons.ActivateTeam.ActivateTeam;

namespace Api.Features.TeamSeasons.ActivateTeam;

public class ActivateTeamHandler(PremPointsDbContext context) : IRequestHandler<Command, Result<TeamSeasonDto>>
{
    public async Task<Result<TeamSeasonDto>> Handle(Command command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var requestedTeam = await context.Teams.SingleAsync(u => u.TeamName == command.TeamName, cancellationToken);
        var effectiveDate = command.AsAtDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var requestedSeason = await SeasonQueries.GetByDateAsync(context, effectiveDate, cancellationToken);
        if (requestedSeason is null)
            return Result.NotFound("");

        var teamSeason = new TeamSeasonEntity { Team = requestedTeam, Season = requestedSeason };

        context.TeamSeasons.Add(teamSeason);
        await context.SaveChangesAsync(cancellationToken);
        return teamSeason.ToDto();
    }
}
