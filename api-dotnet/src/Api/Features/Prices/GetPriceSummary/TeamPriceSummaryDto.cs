using System.Text.Json.Serialization;

namespace Api.Features.Prices.GetPriceSummary;

/// <summary>
/// Which way a club's price moved at the last cut.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PriceMovement
{
    /// Not enough history to say — the club has one price or none.
    Unknown,
    Up,
    Down,
    Level,
}

/// <summary>
/// A club and the most recent price we hold for it.
/// <para>
/// Every club appears whether or not it has a price, because the page this
/// feeds is the price board: a club silently missing from it is how a gap in
/// the data goes unnoticed until someone tries to trade.
/// </para>
/// </summary>
public sealed record TeamPriceSummaryDto
{
    public required Guid TeamId { get; init; }
    public required string TeamName { get; init; }

    public decimal? Bid { get; init; }
    public decimal? Ask { get; init; }
    public decimal? Mid { get; init; }
    public DateOnly? ValueDate { get; init; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public PriceType? PriceType { get; init; }

    /// <summary>The mid before this one, and what <see cref="Movement"/> compares against.</summary>
    public decimal? PreviousMid { get; init; }

    public PriceMovement Movement { get; init; }
}
