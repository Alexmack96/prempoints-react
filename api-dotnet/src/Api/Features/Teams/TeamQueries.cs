using Api.Domain.Entities;
using Api.Infrastructure.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace Api.Features.Teams;

/// <summary>
/// The read-side queries the Teams slices share. Kept as composable
/// <see cref="IQueryable{T}"/> builders rather than eager materialisers so the
/// list endpoint can count and page over the same predicate it filters with,
/// in one round trip each, instead of pulling every team into memory first.
/// </summary>
public static class TeamQueries
{
    public static async Task<TeamEntity?> GetByIdAsync(
        PremPointsDbContext context,
        Guid id,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        return await context.Teams.SingleOrDefaultAsync(t => t.Id == id, ct);
    }

    /// <summary>
    /// Name lookup for slices that legitimately key off the name internally (a
    /// price is submitted for "Arsenal", not for a guid). Deliberately not
    /// exposed as a route: see GetTeamById for why identity is the id.
    /// </summary>
    public static async Task<TeamEntity?> GetByTeamNameAsync(
        PremPointsDbContext context,
        string teamName,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(teamName);
        ArgumentNullException.ThrowIfNull(context);
        return await context.Teams.SingleOrDefaultAsync(t => t.TeamName == teamName, ct);
    }

    public static async Task<bool> NameExistsAsync(
        PremPointsDbContext context,
        string teamName,
        Guid? exceptId = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        // exceptId keeps a rename from colliding with the team being renamed:
        // PUT /teams/{id} with an unchanged name must succeed, not 409.
        return await context.Teams
            .Where(t => t.TeamName == teamName)
            .Where(t => exceptId == null || t.Id != exceptId)
            .AnyAsync(ct);
    }

    /// <summary>
    /// Applies the collection filters. A filter that matches nothing yields an
    /// empty result — never an error, and never a 404.
    /// </summary>
    public static IQueryable<TeamEntity> Filter(
        PremPointsDbContext context,
        Guid? activeInSeasonId,
        string? nameContains)
    {
        ArgumentNullException.ThrowIfNull(context);

        IQueryable<TeamEntity> query = context.Teams.AsNoTracking();

        if (activeInSeasonId is { } seasonId)
        {
            query = query.Where(t => t.TeamSeasons.Any(ts => ts.SeasonId == seasonId));
        }

        if (!string.IsNullOrWhiteSpace(nameContains))
        {
            query = query.Where(t => EF.Functions.Like(t.TeamName, $"%{nameContains}%"));
        }

        return query;
    }

    /// <summary>
    /// Counts what a delete would orphan. Every foreign key into Teams is
    /// <c>DeleteBehavior.Restrict</c>, so without this check the database
    /// rejects the delete and the caller gets a 500 instead of a 409 that says
    /// which relationship blocked it.
    /// </summary>
    public static async Task<TeamReferences> CountReferencesAsync(
        PremPointsDbContext context,
        Guid teamId,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new TeamReferences(
            Seasons: await context.TeamSeasons.CountAsync(ts => ts.TeamId == teamId, ct),
            Prices: await context.Prices.CountAsync(p => p.TeamId == teamId, ct),
            Trades: await context.Trades.CountAsync(t => t.TeamId == teamId, ct));
    }
}

public readonly record struct TeamReferences(int Seasons, int Prices, int Trades)
{
    public bool Any => Seasons > 0 || Prices > 0 || Trades > 0;

    public string Describe() => string.Join(", ", Parts());

    private IEnumerable<string> Parts()
    {
        if (Seasons > 0) yield return $"{Seasons} season membership(s)";
        if (Prices > 0) yield return $"{Prices} price(s)";
        if (Trades > 0) yield return $"{Trades} trade(s)";
    }
}
