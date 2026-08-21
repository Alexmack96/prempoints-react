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

    /// <summary>What a player sells at — the lower side of the spread.</summary>
    public decimal Bid { get; set; }

    /// <summary>What a player buys at — the higher side of the spread.</summary>
    public decimal Ask { get; set; }

    /// <summary>
    /// The price we quote and settle against, midway between bid and ask.
    /// <para>
    /// A persisted computed column rather than a stored value the handler sets.
    /// The mid is entirely determined by the two sides of the spread, so storing
    /// it independently creates a second source of truth that can disagree with
    /// them — and it stays queryable in SQL, which a C# expression body would
    /// not be.
    /// </para>
    /// </summary>
    public decimal Mid { get; private set; }

    public PriceType PriceType { get; set; }
    public DateOnly ValueDate { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTime LastModifiedUtc { get; set; }
    public Guid LastModifiedBy { get; set; }
    public TeamEntity Team { get; set; } = null!;
    public SeasonPeriodEntity SeasonPeriod { get; set; } = null!;
}
