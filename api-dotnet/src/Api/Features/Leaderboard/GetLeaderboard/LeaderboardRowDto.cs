namespace Api.Features.Leaderboard.GetLeaderboard;

/// <summary>
/// One player's standing in a season.
/// <para>
/// Every enrolled player appears, including the ones who have not traded yet.
/// A leaderboard that only lists players with a score reads as if the missing
/// ones are not playing, and on the first day of a season that would be
/// everybody.
/// </para>
/// </summary>
public sealed record LeaderboardRowDto
{
    /// <summary>Position on the board, 1-based. Equal scores share a rank.</summary>
    public required int Rank { get; init; }

    public required Guid UserId { get; init; }
    public required string Username { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }

    /// <summary>
    /// Season profit and loss. Zero for a player who has not traded, and — until
    /// trades are marked against a settled price — zero for everyone else too.
    /// See <see cref="GetLeaderboardHandler"/>.
    /// </summary>
    public required decimal Pnl { get; init; }

    /// <summary>How many trades the player has placed this season.</summary>
    public required int TradesPlaced { get; init; }

    /// <summary>
    /// Whether <see cref="Pnl"/> is a settled number or a placeholder. False
    /// while settlement is unimplemented, so a client can say "not scored yet"
    /// rather than presenting a zero as a real result.
    /// </summary>
    public required bool PnlIsSettled { get; init; }
}
