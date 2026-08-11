using Api.Domain.Contracts;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Domain.Entities;

public class UserSeasonEntity : IAuditableEntity
{
    [Column("UserSeasonId")]
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid SeasonId { get; set; }
    public int LateJoinerFee { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTime LastModifiedUtc { get; set; }
    public Guid LastModifiedBy { get; set; }
    //Navigation Properties
    public UserEntity User { get; set; } = null!;
    public SeasonEntity Season { get; set; } = null!;
}