using Api.Domain.Contracts;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Domain.Entities;

public class TeamEntity : IAuditableEntity
{
    [Column("TeamId")]
    public Guid Id { get; set; }
    public required string TeamName { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTime LastModifiedUtc { get; set; }
    public Guid LastModifiedBy { get; set; }
    public ICollection<TeamSeasonEntity> TeamSeasons { get; } = new List<TeamSeasonEntity>();
}