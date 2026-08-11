using Api.Domain.Contracts;
using Api.Features.Trades;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Domain.Entities;

public class TradeEntity : IAuditableEntity
{
    [Column("TradeId")]
    public Guid Id { get; set; }
    public Guid PriceId { get; set; }
    public Guid SeasonPeriodId { get; set; }
    public Guid UserId { get; set; }
    public Guid TeamId { get; set; }
    public int Exposure { get; set; }
    public TradeType TradeType { get; set; }
    public DateTime TradeDateUtc { get; set; }
    public required string TimezoneIana { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTime LastModifiedUtc { get; set; }
    public Guid LastModifiedBy { get; set; }
    //Navigation Properties
    public PriceEntity Price { get; set; } = null!;
    public SeasonPeriodEntity SeasonPeriod { get; set; } = null!;
    public UserEntity User { get; set; } = null!;
    public TeamEntity Team { get; set; } = null!;
}