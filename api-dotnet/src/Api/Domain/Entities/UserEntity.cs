using Api.Domain.Authorization;
using Api.Domain.Contracts;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Domain.Entities;

public class UserEntity : IAuditableEntity
{
    [Column("UserId")]
    public Guid Id { get; set; }
    // NEW: This links your local user to the WorkOS user.
    // WorkOS IDs look like: "user_01H2X..."
    public required string WorkOSUserId { get; set; }
    public required string Username { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public UserRole Role { get; set; }

    /// <summary>
    /// Whether the player picked this username, as opposed to UserProvisioner
    /// deriving one from their name so that first sign-in was not blocked.
    /// <para>
    /// This is what the onboarding gate keys off. Without it there is no way to
    /// tell a generated name from a chosen one that happens to look the same,
    /// and the app would either nag someone who already decided or never ask.
    /// </para>
    /// </summary>
    public bool UsernameChosen { get; set; }

    /// <summary>
    /// The club whose badge shows against this player. Nullable because a user
    /// row exists from first sign-in, before they have been asked.
    /// </summary>
    public Guid? FavouriteTeamId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTime LastModifiedUtc { get; set; }
    public Guid LastModifiedBy { get; set; }
    public TeamEntity? FavouriteTeam { get; set; }
    public ICollection<UserSeasonEntity> UserSeasons { get; } = new List<UserSeasonEntity>();
}
