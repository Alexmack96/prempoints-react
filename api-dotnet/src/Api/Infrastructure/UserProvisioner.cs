using Api.Domain.Authorization;
using Api.Domain.Entities;
using Api.Infrastructure.EntityFramework;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Api.Infrastructure;

/// <summary>
/// The custom claims a WorkOS JWT template puts on the access token.
/// <para>
/// An AuthKit access token carries <c>sub</c>, <c>org_id</c>, <c>sid</c> and
/// <c>jti</c> and nothing else — no email, no name. Those have to be added
/// deliberately in the WorkOS dashboard under JWT template:
/// </para>
/// <code>
/// {
///   "urn:prempoints:email":      "{{ user.email }}",
///   "urn:prempoints:first_name": "{{ user.first_name }}",
///   "urn:prempoints:last_name":  "{{ user.last_name }}"
/// }
/// </code>
/// <para>
/// URN-prefixed on purpose. A bare "email" risks colliding with a claim WorkOS
/// adds later, and the prefix says at a glance which claims are ours.
/// </para>
/// </summary>
public static class WorkOsProfileClaims
{
    public const string Email = "urn:prempoints:email";
    public const string FirstName = "urn:prempoints:first_name";
    public const string LastName = "urn:prempoints:last_name";
}

public interface IUserProvisioner
{
    Task<UserEntity?> ResolveAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default);
}

/// <summary>
/// Maps a validated WorkOS identity onto the PremPoints user row, creating that
/// row the first time someone signs in.
/// <para>
/// Sign-up is invitation-only, so every WorkOS identity that reaches here is one
/// an administrator deliberately invited. That is what makes creating the row
/// automatically safe: there is no such thing as an unwanted account, so there
/// is nothing for a human to approve afterwards.
/// </para>
/// <para>
/// It creates the user and nothing else. Being a user is not the same as
/// playing a season — ActivateUser owns that, it is Admin-only, and keeping the
/// two separate is also what keeps this off the season lookup that would
/// otherwise make signing up out of season fail.
/// </para>
/// </summary>
public sealed class UserProvisioner(
    PremPointsDbContext context,
    TimeProvider clock,
    ILogger<UserProvisioner> logger) : IUserProvisioner
{
    public async Task<UserEntity?> ResolveAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(principal);

        var externalId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(externalId))
        {
            return null;
        }

        var existing = await context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.WorkOSUserId == externalId, cancellationToken);

        if (existing is not null)
        {
            return existing;
        }

        var firstName = principal.FindFirst(WorkOsProfileClaims.FirstName)?.Value;
        var lastName = principal.FindFirst(WorkOsProfileClaims.LastName)?.Value;

        // Used to name the account and then dropped. The email is never
        // persisted: WorkOS holds it, nothing here queries or joins on it, and
        // a copy would only be a second version to go stale.
        var email = principal.FindFirst(WorkOsProfileClaims.Email)?.Value;

        // All three absent means the JWT template is not configured, and there
        // is nothing to build a name from. Degrade to how this behaved before —
        // authenticated, with no player attached — rather than failing the
        // request, and say why so the cause is in the logs rather than a guess.
        if (string.IsNullOrWhiteSpace(firstName)
            && string.IsNullOrWhiteSpace(lastName)
            && string.IsNullOrWhiteSpace(email))
        {
            logger.LogWarning(
                "No profile claims on the token for {ExternalId}, so no user row was created. " +
                "Add the JWT template in the WorkOS dashboard — see WorkOsProfileClaims.",
                externalId);

            return null;
        }

        var userId = Guid.CreateVersion7(clock.GetUtcNow());
        var user = new UserEntity
        {
            Id = userId,
            WorkOSUserId = externalId,
            Username = await AllocateUsernameAsync(firstName, lastName, email, cancellationToken),

            // WorkOS allows both to be empty — a Google account carrying only a
            // display name, or an invite accepted without filling the form in.
            // The columns are required, so fall back rather than reject someone
            // whose only sin is a sparse profile. They can be corrected later.
            FirstName = Fallback(firstName, "Unknown"),
            LastName = Fallback(lastName, "Unknown"),

            Role = UserRole.Standard,

            // Self-referential: this row is the first record of this person, so
            // they are their own author. The audit
            // interceptor fills these from the InternalUserId claim, which does
            // not exist yet — this row is what creates it.
            CreatedBy = userId,
            LastModifiedBy = userId,
        };

        context.Users.Add(user);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Two requests from a brand-new user can race here — a page that
            // fires several queries on load is enough. The unique indexes mean
            // one insert wins and the other lands here; the winner's row is the
            // right answer for both.
            context.ChangeTracker.Clear();

            return await context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.WorkOSUserId == externalId, cancellationToken);
        }

        logger.LogInformation("Created user {UserId} for WorkOS identity {ExternalId}.", userId, externalId);

        return user;
    }

    private static string Fallback(string? value, string whenMissing) =>
        string.IsNullOrWhiteSpace(value) ? whenMissing : value.Trim();

    /// <summary>
    /// Picks a free username, since WorkOS has no username of its own and the
    /// column is uniquely indexed.
    /// <para>
    /// Built from the name rather than the email, because the username is shown
    /// on the leaderboard: deriving it from the address would print the local
    /// part of someone's email to everyone in the league. The email is only a
    /// fallback for a profile carrying no name at all.
    /// </para>
    /// <para>
    /// Two people resolving to the same name is ordinary, not an error, so the
    /// second gets a suffix rather than a failed sign-in.
    /// </para>
    /// </summary>
    private async Task<string> AllocateUsernameAsync(
        string? firstName,
        string? lastName,
        string? email,
        CancellationToken cancellationToken)
    {
        var fromName = $"{firstName}{lastName}";
        var source = string.IsNullOrWhiteSpace(fromName)
            ? (email ?? string.Empty).Split('@', 2)[0]
            : fromName;

        var cleaned = new string([.. source.Where(char.IsLetterOrDigit)]);

        // Validators cap usernames at 50, and the suffix has to fit inside that
        // too, so leave room for it rather than truncating a name into a
        // collision with the one above it.
        var seed = cleaned.Length == 0 ? "player" : cleaned;
        var baseName = seed[..Math.Min(seed.Length, 45)];

        var taken = await context.Users
            .AsNoTracking()
            .Where(u => u.Username.StartsWith(baseName))
            .Select(u => u.Username)
            .ToListAsync(cancellationToken);

        if (!taken.Contains(baseName, StringComparer.OrdinalIgnoreCase))
        {
            return baseName;
        }

        for (var suffix = 2; suffix < 10000; suffix++)
        {
            var candidate = $"{baseName}{suffix}";
            if (!taken.Contains(candidate, StringComparer.OrdinalIgnoreCase))
            {
                return candidate;
            }
        }

        // Unreachable short of ten thousand people sharing a local part, but a
        // username has to come back and the WorkOS id is unique by definition.
        return $"{baseName}-{Guid.CreateVersion7(clock.GetUtcNow())}"[..50];
    }
}
