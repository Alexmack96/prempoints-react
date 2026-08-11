using Api.Domain.Contracts;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Domain.Entities;

public class SeasonEntity : IAuditableEntity
{
    [Column("SeasonId")]
    public Guid Id { get; set; }
    public required string SeasonName { get; set; }
    public int StartYear { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTime LastModifiedUtc { get; set; }
    public Guid LastModifiedBy { get; set; }
    public ICollection<SeasonPeriodEntity> SeasonPeriods { get; } = new List<SeasonPeriodEntity>();
    public ICollection<TeamSeasonEntity> TeamSeasons { get; } = new List<TeamSeasonEntity>();
    public ICollection<UserSeasonEntity> UserSeasons { get; } = new List<UserSeasonEntity>();

}
