namespace Api.Features.Trades.GetJokerAllowance;

/// <summary>A joker already spent this season.</summary>
public sealed record JokerUseDto
{
    public required int CalendarYear { get; init; }
    public required DateTime PlayedOnUtc { get; init; }
}

/// <summary>
/// Whether this player may play a joker on a given date, and what they have
/// already spent this season.
/// <para>
/// <see cref="Available"/> answers the board's actual question. The rest is
/// there so the UI can explain itself rather than just greying a box out.
/// </para>
/// </summary>
public sealed record JokerAllowanceDto
{
    public required Guid SeasonId { get; init; }
    public required string SeasonName { get; init; }

    /// <summary>The calendar year the requested trade date falls in.</summary>
    public required int CalendarYear { get; init; }

    /// <summary>Whether a joker may be played on the requested date.</summary>
    public required bool Available { get; init; }

    /// <summary>
    /// The joker that blocks this date, if one does. Null when
    /// <see cref="Available"/> is true — including when the player already has a
    /// joker on this very date, because editing that submission is not spending
    /// a second one.
    /// </summary>
    public DateTime? BlockedByUtc { get; init; }

    /// <summary>Every joker spent this season, one per calendar year.</summary>
    public required IReadOnlyList<JokerUseDto> PlayedThisSeason { get; init; }
}
