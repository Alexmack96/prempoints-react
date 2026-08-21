namespace IntegrationTests.Features;

/// <summary>
/// Endpoints that break a convention and are known to.
/// <para>
/// Every entry is debt, not permission. Listing them here is what lets the rule
/// be switched on today: the alternative was leaving the rule unwritten, which
/// is how ten endpoints ended up with no rate limiter. A new endpoint cannot
/// join these lists without someone deliberately adding it, which is the point.
/// </para>
/// <para>
/// These close as the Teams template is rolled out to the other resources.
/// </para>
/// </summary>
internal static class ConventionDebt
{
    /// Creates that answer 200 instead of 201 with a Location header. Most of
    /// these have nowhere to point a Location at yet, because the resource has
    /// no item route — which is the same rollout gap seen from the other side.
    public static readonly Dictionary<string, string> CreateReturnsOk = new(StringComparer.Ordinal)
    {
        ["api/v1/prices"] = "No GET /prices/{id} to point Location at.",
        ["api/v1/users"] = "No GET /users/{id}; identity is still the username.",
        ["api/v1/seasons"] = "No GET /seasons/{id}.",
        ["api/v1/seasonPeriods"] = "No GET /seasonPeriods/{id}.",
        ["api/v1/seednewseason"] = "An admin action, not a resource collection.",
    };

    /// Collection reads returning a bare array rather than PagedResponse<T>.
    public static readonly Dictionary<string, string> UnpagedCollection = new(StringComparer.Ordinal)
    {
        ["api/v1/users/active"] = "Should become GET /users?activeOn=, paged, during rollout.",
        ["api/v1/pnl/trade/{username?}"] = "A report rather than a resource collection; will still need paging once a season's worth of trades accumulates.",
    };

    /// Routes identifying a resource by something other than its id.
    public static readonly Dictionary<string, string> NonIdIdentity = new(StringComparer.Ordinal)
    {
        ["api/v1/users/{username}"] = "Username is mutable; should be GET /users/{id:guid}.",
        ["api/v1/users/activate/{username}"] = "Should be POST /users/{id:guid}/seasons.",
        ["api/v1/users/deactivate/{username}"] = "Should be DELETE /users/{id:guid}/seasons/{seasonId:guid}.",
        ["api/v1/teams/activate/{teamName}"] = "Should be POST /teams/{id:guid}/seasons.",
        ["api/v1/pnl/trade/{username?}"] = "Optional username acts as a filter; belongs in the query string.",
    };

    /// <summary>
    /// Writes reachable without authentication.
    /// <para>
    /// Closed, and deliberately kept empty rather than deleted. Every entry
    /// that was here changed game state — reseeding a season, enrolling teams,
    /// reclassifying trades — and all seven now require
    /// <see cref="Api.Domain.Authorization.Policies.Admin"/>. An empty list
    /// keeps <c>WritesRequireAuthorization</c> enforcing with no exemptions,
    /// and makes adding one an obvious, deliberate act rather than an omission.
    /// </para>
    /// </summary>
    public static readonly Dictionary<string, string> AnonymousWrite = new(StringComparer.Ordinal);
}
