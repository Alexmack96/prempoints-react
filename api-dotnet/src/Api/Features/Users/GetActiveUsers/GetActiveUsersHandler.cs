using Api.Features.Seasons;
using Api.Infrastructure.EntityFramework;
using Ardalis.Result;
using MediatR;
using static Api.Features.Users.GetActiveUsers.GetActiveUsers;

namespace Api.Features.Users.GetActiveUsers;

public class GetActiveUsersHandler(PremPointsDbContext context) : IRequestHandler<Query, Result<List<UserDto>>>
{
    public async Task<Result<List<UserDto>>> Handle(Query command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var effectiveDate = command.AsAtDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var season = await SeasonQueries.GetByDateAsync(context, effectiveDate, cancellationToken);
        if (season is null)
            return Result.NotFound($"No season found covering the date {effectiveDate}.");

        var activeUsers = await UserQueries.GetActiveBySeasonIdAsync(context, season.Id, cancellationToken);
        if (activeUsers is null)
            return Result.NotFound("No Active Users", $"No active teams found in the db.");

        return activeUsers.ToDtos();
    }
}
