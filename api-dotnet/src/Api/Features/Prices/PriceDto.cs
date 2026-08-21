using Api.Domain.Contracts;
using System.Text.Json.Serialization;

namespace Api.Features.Prices;

public record PriceDto : IEntityDto
{
    public Guid Id { get; init; }
    public Guid TeamId { get; init; }
    public Guid SeasonPeriodId { get; init; }

    /// Sell side.
    public decimal Bid { get; init; }

    /// Buy side.
    public decimal Ask { get; init; }

    /// Midway between the two, and the number we quote. Derived, never sent in.
    public decimal Mid { get; init; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public PriceType PriceType { get; init; }
    public required DateOnly ValueDate { get; init; }
}
