using Api.Infrastructure.EntityFramework;
using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static Api.Features.Users.GetUserMe.GetUserMe;

namespace Api.Features.Users.GetUserMe;

public class GetUserMeHandler(ILogger<GetUserMeHandler> logger, PremPointsDbContext context)
    : IRequestHandler<Query, Result<UserDto>>
{
    public async Task<Result<UserDto>> Handle(Query query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        // Assuming your User entity has a field for the Auth Provider ID (e.g., WorkOS ID)
        // If you have a 'UserQueries' helper, you can use that here instead.
        var userEntity = await context.Users
            .AsNoTracking() // Read-only optimization
            .FirstOrDefaultAsync(u => u.WorkOSUserId == query.WorkOSUserId, cancellationToken);

        if (userEntity is null)
        {
            logger.LogWarning("User authenticated with AuthId {AuthId} but does not exist in local DB.", query.WorkOSUserId);

            // 404 here is useful: it tells the frontend "You are logged in, but you need to finish onboarding/create a profile."
            return Result.NotFound();
        }

        return userEntity.ToDto();
    }
}