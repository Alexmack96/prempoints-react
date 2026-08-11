namespace Api.Features.GetPnlDetails;

public record PnlByTrade
{
    public Guid TradeId { get; init; }
    public int SeasonStartYear { get; init; }
    public int GameweekNumber { get; init; }
    public required string Username { get; init; }
    public required string TeamName { get; init; }
    public int Exposure { get; init; }
    public decimal TradePrice { get; init; }
    public decimal IndexPriceProvisional { get; init; }
    public decimal IndexPriceFinal { get; init; }
    public int TradeMultiplier { get; init; }
    public DateTime TradeDateUtc { get; init; }
    public required string TimezoneIana { get; init; }
    public decimal PnlValue { get; init; }
}