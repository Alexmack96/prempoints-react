namespace Api.Features.Trades;

/// <summary>
/// The rules of the game, in one place.
/// <para>
/// These are duplicated in the client so the board can guide a player as they
/// pick rather than rejecting the whole submission at the end. The copy here is
/// the one that decides: a client can be edited, and the numbers below are what
/// every trade is actually checked against.
/// </para>
/// </summary>
public static class TradingRules
{
    /// <summary>
    /// Every submission stakes exactly this much across its positions, so
    /// <c>|X| + |Y| = 40</c>. A player choosing where to put their forty is the
    /// whole decision; letting them stake less would just be a smaller bet on
    /// the same view.
    /// </summary>
    public const int TotalStake = 40;

    /// <summary>Stakes move in fives.</summary>
    public const int StakeIncrement = 5;

    /// <summary>The smallest position worth taking.</summary>
    public const int MinStake = 5;

    /// <summary>At most two clubs may be backed.</summary>
    public const int MaxPositions = 2;

    /// <summary>
    /// Jokers allowed per calendar year, within a season.
    /// <para>
    /// Scoped to both because a season straddles New Year: one before Christmas
    /// and one after makes two per season, while two dates in the same calendar
    /// year are still fine if they belong to different seasons.
    /// </para>
    /// </summary>
    public const int JokersPerCalendarYearPerSeason = 1;
}
