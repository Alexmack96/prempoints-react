using Api.Features.Seasons;
using Api.Infrastructure.EntityFramework;
using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static Api.Features.UserSeasons.DeactivateUser.DeactivateUser;

namespace Api.Features.UserSeasons.DeactivateUser;

public class DeactivateUserHandler(PremPointsDbContext context) : IRequestHandler<Command, Result> // Changed to non-generic Result
{
    public async Task<Result> Handle(Command command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var requestedUser = await context.Users.SingleOrDefaultAsync(u => u.Username == command.Username, cancellationToken);
        if (requestedUser is null)
            return Result.NotFound($"User '{command.Username}' not found.");

        var effectiveDate = command.AsAtDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var requestedSeason = await SeasonQueries.GetByDateAsync(context, effectiveDate, cancellationToken);
        if (requestedSeason is null)
            return Result.NotFound("No valid season found for this date.");

        var userSeason = await context.UserSeasons
            .SingleOrDefaultAsync(us => us.SeasonId == requestedSeason.Id && us.UserId == requestedUser.Id, cancellationToken);

        if (userSeason is null)
            return Result.NotFound("User is not part of this season.");

        context.UserSeasons.Remove(userSeason);

        var allUsersTrades = context.Trades.Where(tr => tr.UserId == requestedUser.Id);
        var seasonPeriodsIds = context.SeasonPeriods.Where(sp => sp.SeasonId == requestedSeason.Id).Select(sp => sp.Id);

        var tradesToRemove = allUsersTrades.Where(x => seasonPeriodsIds.Contains(x.SeasonPeriodId));
        context.Trades.RemoveRange(tradesToRemove);

        await context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
