using Api.Infrastructure.EntityFramework;
using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static Api.Features.Users.GetUserByUsername.GetUserByUsername;

namespace Api.Features.Users.GetUserByUsername;

public class GetUserByUsernameHandler(ILogger<GetUserByUsernameHandler> logger, PremPointsDbContext context) : IRequestHandler<Query, Result<UserDto>>
{
    public async Task<Result<UserDto>> Handle(Query query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var userEntity = await UserQueries.GetByUsernameAsync(context, query.Username, cancellationToken);
        if (userEntity is null)
        {
            logger.LogWarning("Unable to find user with name: {UserName}", query.Username);
            return Result.NotFound();
        }

        return userEntity.ToDto();
    }
}
