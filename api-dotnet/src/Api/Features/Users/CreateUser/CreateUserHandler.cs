using Api.Domain.Entities;
using Api.Features.Seasons;
using Api.Infrastructure;
using Api.Infrastructure.EntityFramework;
using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static Api.Features.Users.CreateUser.CreateUser;

namespace Api.Features.Users.CreateUser;

public class CreateUserHandler(PremPointsDbContext context, TimeProvider clock) : IRequestHandler<Command, Result<UserDto>>
{
    public async Task<Result<UserDto>> Handle(Command command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var userExists = await context.Users.AnyAsync(t => t.Username == command.Username, cancellationToken);
        if (userExists)
            return Result.Conflict("DuplicateName", $"User '{command.Username}' already exists.");

        var today = clock.UtcToday();
        var season = await SeasonQueries.GetByDateAsync(context, today, cancellationToken);
        if (season is null)
            return Result.NotFound("Season Not Found", $"No valid season found at: '{today}'.");

        var newUserId = Guid.CreateVersion7(clock.GetUtcNow());
        var entity = new UserEntity
        {
            Id = newUserId,
            WorkOSUserId = command.WorkOSUserId,
            Username = command.Username,
            Email = command.Email,
            FirstName = command.FirstName,
            LastName = command.LastName,
            Role = command.Role,
            CreatedBy = newUserId,
            LastModifiedBy = newUserId,
        };

        var userSeason = new UserSeasonEntity { Id = Guid.CreateVersion7(clock.GetUtcNow()), User = entity, Season = season };

        context.Users.Add(entity);
        context.UserSeasons.Add(userSeason);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Result.Conflict("DuplicateName", $"User '{command.Username}' already exists.");
        }

        return entity.ToDto();
    }
}
