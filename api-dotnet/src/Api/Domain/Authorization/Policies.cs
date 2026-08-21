namespace Api.Domain.Authorization;

/// <summary>
/// Named authorization policies, so an endpoint says
/// <c>RequireAuthorization(Policies.Admin)</c> rather than repeating a magic
/// string that a typo silently turns into "deny everyone".
/// <para>
/// <see cref="UserRole"/> lives on the user row in our database, not in the
/// WorkOS token, so the role claim these policies read is added during token
/// validation from that row. See <c>AddWorkOsAuthentication</c>.
/// </para>
/// </summary>
public static class Policies
{
    /// <summary>Write access to reference data — creating, renaming and deleting teams.</summary>
    public const string Admin = "Admin";
}
