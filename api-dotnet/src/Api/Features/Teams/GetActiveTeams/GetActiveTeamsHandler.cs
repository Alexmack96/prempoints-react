using Api.Features.Seasons;
using Api.Infrastructure.EntityFramework;
using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static Api.Features.Teams.GetActiveTeams.GetActiveTeams;

namespace Api.Features.Teams.GetActiveTeams;

public class GetActiveTeamsHandler(ILogger<GetActiveTeamsHandler> logger, PremPointsDbContext context) : IRequestHandler<Query, Result<List<TeamDto>>>
{
    public async Task<Result<List<TeamDto>>> Handle(Query command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var effectiveDate = command.AsAtDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var currentSeason = await SeasonQueries.GetByDateAsync(context, effectiveDate, cancellationToken);
        if (currentSeason is null)
        {
            logger.LogWarning("Unable to find valid period as at date: {AsAtDate}", command.AsAtDate);
            return Result.NotFound();
        }

        var teams = await TeamQueries.GetActiveBySeasonIdAsync(context, currentSeason.Id, cancellationToken);
        if (teams.Count == 0)
        {
            logger.LogCritical("CRITICAL: In valid periods we should always have active teams.");
            return Result.CriticalError("CRITICAL: In valid periods we should always have active teams.");
        }

        return teams.ToDtos();
    }
}
