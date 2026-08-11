using Api.Domain.Contracts;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Domain.Entities;

public class TeamSeasonEntity : IAuditableEntity
{
    [Column("TeamSeasonId")]
    public Guid Id { get; set; }
    public Guid TeamId { get; set; }
    public Guid SeasonId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTime LastModifiedUtc { get; set; }
    public Guid LastModifiedBy { get; set; }
    //Navigation Properties
    public TeamEntity Team { get; set; } = null!;
    public SeasonEntity Season { get; set; } = null!;
}