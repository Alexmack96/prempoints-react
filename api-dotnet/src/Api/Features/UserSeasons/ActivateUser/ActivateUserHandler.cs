using Api.Domain.Entities;
using Api.Features.Seasons;
using Api.Infrastructure.EntityFramework;
using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static Api.Features.UserSeasons.ActivateUser.ActivateUser;

namespace Api.Features.UserSeasons.ActivateUser;

public class ActivateUserHandler(PremPointsDbContext context) : IRequestHandler<Command, Result<UserSeasonDto>>
{
    public async Task<Result<UserSeasonDto>> Handle(Command command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var requestedUser = await context.Users.SingleAsync(u => u.Username == command.Username, cancellationToken);
        var effectiveDate = command.AsAtDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var requestedSeason = await SeasonQueries.GetByDateAsync(context, effectiveDate, cancellationToken)
            ?? throw new InvalidOperationException("No valid season in this daterange.");

        var userSeason = new UserSeasonEntity { UserId = requestedUser.Id, SeasonId = requestedSeason.Id, LateJoinerFee = command.LateJoinerFee };

        context.UserSeasons.Add(userSeason);
        await context.SaveChangesAsync(cancellationToken);
        return userSeason.ToDto();
    }
}
