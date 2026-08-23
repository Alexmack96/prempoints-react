using Api.Infrastructure.EntityFramework;
using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static Api.Features.Users.UpdateMyProfile.UpdateMyProfile;

namespace Api.Features.Users.UpdateMyProfile;

public class UpdateMyProfileHandler(PremPointsDbContext context) : IRequestHandler<Command, Result<UserDto>>
{
    public async Task<Result<UserDto>> Handle(Command command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var user = await context.Users
            .Include(u => u.FavouriteTeam)
            .FirstOrDefaultAsync(u => u.Id == command.UserId, cancellationToken);

        if (user is null)
        {
            return Result.NotFound("No PremPoints player is linked to this sign-in.");
        }

        // Asked before saving so a taken name comes back as a 409 the picker can
        // show against the field, rather than a constraint violation from deep
        // inside SaveChanges. The unique index is still the real guarantee —
        // see the catch below — this is for the message.
        var nameTaken = await context.Users.AnyAsync(
            other => other.Id != command.UserId && other.Username == command.Username,
            cancellationToken);

        if (nameTaken)
        {
            return Result.Conflict("UsernameTaken", $"'{command.Username}' is already taken.");
        }

        if (command.FavouriteTeamId is { } teamId)
        {
            var teamExists = await context.Teams.AnyAsync(team => team.Id == teamId, cancellationToken);

            if (!teamExists)
            {
                return Result.NotFound("TeamNotFound", "That club is not in this league.");
            }
        }

        user.Username = command.Username;
        user.FavouriteTeamId = command.FavouriteTeamId;

        // The flag is the point of the endpoint: it is what stops the
        // onboarding gate asking again. Set whether or not the name changed,
        // because keeping the generated one is a decision too.
        user.UsernameChosen = true;

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Two players claiming the same name between the check above and
            // this save. Rare, and the index is what actually prevents it.
            return Result.Conflict("UsernameTaken", $"'{command.Username}' is already taken.");
        }

        // Reloaded so the response carries the club's name rather than only the
        // id the caller already sent.
        await context.Entry(user).Reference(u => u.FavouriteTeam).LoadAsync(cancellationToken);

        return user.ToDto();
    }
}
