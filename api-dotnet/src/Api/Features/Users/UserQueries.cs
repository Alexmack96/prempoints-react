using Api.Domain.Entities;
using Api.Infrastructure.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace Api.Features.Users;

public static class UserQueries
{
    public static async Task<UserEntity?> GetByUsernameAsync(PremPointsDbContext context, string username, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentNullException.ThrowIfNull(context);

        return await context.Users.SingleOrDefaultAsync(t => t.Username == username, ct);
    }
    public static async Task<UserEntity?> GetByWorkOSIdAsync(PremPointsDbContext context, string workOSIdAsync, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workOSIdAsync);
        ArgumentNullException.ThrowIfNull(context);

        return await context.Users.SingleOrDefaultAsync(t => t.WorkOSUserId == workOSIdAsync, ct);
    }
    public static async Task<List<UserEntity>> GetActiveBySeasonIdAsync(
        PremPointsDbContext context,
        Guid seasonId,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var activeUserIds = await context.UserSeasons
            .Where(ts => ts.SeasonId == seasonId)
            .Select(ts => ts.UserId)
            .ToListAsync(ct);

        return await context.Users
            .Where(t => activeUserIds.Contains(t.Id))
            .ToListAsync(ct);
    }
}
