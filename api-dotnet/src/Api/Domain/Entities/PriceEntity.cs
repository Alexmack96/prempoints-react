using Api.Domain.Contracts;
using Api.Features.Prices;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Domain.Entities;

public class PriceEntity : IAuditableEntity
{
    [Column("PriceId")]
    public Guid Id { get; set; }
    public Guid TeamId { get; set; }
    public Guid SeasonPeriodId { get; set; }
    public decimal Price { get; set; }
    public PriceType PriceType { get; set; }
    public DateOnly ValueDate { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTime LastModifiedUtc { get; set; }
    public Guid LastModifiedBy { get; set; }
    public TeamEntity Team { get; set; } = null!;
    public SeasonPeriodEntity SeasonPeriod { get; set; } = null!;
}