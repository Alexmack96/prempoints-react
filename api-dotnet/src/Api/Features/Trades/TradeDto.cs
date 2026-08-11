using Api.Domain.Contracts;
using System.Text.Json.Serialization;

namespace Api.Features.Trades;

public record TradeDto : IEntityDto
{
    public Guid Id { get; init; }
    public Guid PriceId { get; init; }
    public Guid SeasonPeriodId { get; init; }
    public Guid UserId { get; init; }
    public Guid TeamId { get; init; }
    public int Exposure { get; init; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TradeType TradeType { get; init; }
    public DateTime TradeDateUtc { get; init; }
    public required string TimezoneIana { get; init; }
}