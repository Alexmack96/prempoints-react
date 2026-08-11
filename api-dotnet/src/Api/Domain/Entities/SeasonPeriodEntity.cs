using Api.Domain.Contracts;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Domain.Entities;

public class SeasonPeriodEntity : IAuditableEntity
{
    [Column("SeasonPeriodId")]
    public Guid Id { get; set; }
    public Guid SeasonId { get; set; }
    public required int GameweekNumber { get; set; }
    public DateOnly PeriodStartDate { get; set; }
    public DateOnly PeriodEndDate { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTime LastModifiedUtc { get; set; }
    public Guid LastModifiedBy { get; set; }
    //Navigation Properties
    public SeasonEntity Season { get; set; } = null!;

}