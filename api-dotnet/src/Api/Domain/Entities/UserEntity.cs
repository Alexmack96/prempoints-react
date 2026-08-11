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
    public required string Email { get; set; }
    public UserRole Role { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTime LastModifiedUtc { get; set; }
    public Guid LastModifiedBy { get; set; }
    public ICollection<UserSeasonEntity> UserSeasons { get; } = new List<UserSeasonEntity>();
}
