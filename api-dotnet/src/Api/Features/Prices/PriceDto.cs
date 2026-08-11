using Api.Domain.Contracts;
using System.Text.Json.Serialization;

namespace Api.Features.Prices;

public record PriceDto : IEntityDto
{
    public Guid Id { get; init; }
    public Guid TeamId { get; init; }
    public Guid SeasonPeriodId { get; init; }
    public decimal Price { get; init; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public PriceType PriceType { get; init; }
    public required DateOnly ValueDate { get; init; }
}