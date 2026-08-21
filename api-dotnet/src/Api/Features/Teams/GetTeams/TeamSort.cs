using Api.Domain.Entities;

namespace Api.Features.Teams.GetTeams;

/// <summary>
/// The sort keys this endpoint accepts, as an allow-list.
/// <para>
/// An allow-list rather than reflection over the entity: a sort parameter that
/// maps straight onto property names lets a caller order by columns that are
/// not indexed, and leaks the storage model into the public contract. A leading
/// <c>-</c> means descending.
/// </para>
/// </summary>
public static class TeamSort
{
    public const string Default = "teamName";
    public const string Allowed = "teamName, -teamName, createdAt, -createdAt";

    public static bool IsValid(string? sort) =>
        sort is null || Keys.Contains(Key(sort));

    public static IOrderedQueryable<TeamEntity> Apply(IQueryable<TeamEntity> query, string? sort)
    {
        ArgumentNullException.ThrowIfNull(query);

        var descending = sort?.StartsWith('-') == true;

        // Every branch adds ThenBy(Id): without a tiebreaker, rows with equal
        // sort keys can land on different pages across requests, so offset
        // paging silently repeats or skips them.
        return (Key(sort), descending) switch
        {
            ("createdat", true) => query.OrderByDescending(t => t.CreatedAtUtc).ThenBy(t => t.Id),
            ("createdat", false) => query.OrderBy(t => t.CreatedAtUtc).ThenBy(t => t.Id),
            (_, true) => query.OrderByDescending(t => t.TeamName).ThenBy(t => t.Id),
            _ => query.OrderBy(t => t.TeamName).ThenBy(t => t.Id),
        };
    }

    private static readonly HashSet<string> Keys =
        new(StringComparer.OrdinalIgnoreCase) { "teamname", "createdat" };

    private static string Key(string? sort) =>
        (string.IsNullOrWhiteSpace(sort) ? Default : sort).TrimStart('-').ToLowerInvariant();
}
