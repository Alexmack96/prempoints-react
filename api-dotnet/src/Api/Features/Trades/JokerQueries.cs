using Api.Infrastructure.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace Api.Features.Trades;

/// <summary>
/// The joker allowance, in one place.
/// <para>
/// Both the write path (rejecting a second joker) and the read path (telling
/// the board whether the checkbox is live) go through here. Written twice they
/// would drift, and the failure mode is the worst kind: a UI that offers
/// something the API then refuses.
/// </para>
/// </summary>
public static class JokerQueries
{
    /// <summary>
    /// The date of the joker that blocks <paramref name="tradeDateUtc"/>, or
    /// null if one may be played then.
    /// <para>
    /// A joker on the same date is not a blocker — that is the submission being
    /// edited, not a second joker.
    /// </para>
    /// </summary>
    public static async Task<DateTime?> FindBlockingJokerAsync(
        PremPointsDbContext context,
        Guid userId,
        Guid seasonId,
        DateTime tradeDateUtc,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var year = tradeDateUtc.Year;

        var blocking = await context.Trades
            .AsNoTracking()
            .Where(trade => trade.UserId == userId)
            .Where(trade => trade.TradeType == TradeType.Joker)
            .Where(trade => trade.SeasonPeriod.SeasonId == seasonId)
            .Where(trade => trade.TradeDateUtc.Year == year)
            .Where(trade => trade.TradeDateUtc.Date != tradeDateUtc.Date)
            .Select(trade => trade.TradeDateUtc)
            .FirstOrDefaultAsync(ct);

        return blocking == default ? null : blocking;
    }

    /// <summary>
    /// Every joker this player has spent in a season, one per calendar year.
    /// </summary>
    public static async Task<List<(int CalendarYear, DateTime PlayedOn)>> GetPlayedInSeasonAsync(
        PremPointsDbContext context,
        Guid userId,
        Guid seasonId,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var played = await context.Trades
            .AsNoTracking()
            .Where(trade => trade.UserId == userId)
            .Where(trade => trade.TradeType == TradeType.Joker)
            .Where(trade => trade.SeasonPeriod.SeasonId == seasonId)
            .Select(trade => trade.TradeDateUtc)
            .Distinct()
            .ToListAsync(ct);

        // Grouped after loading: a submission is several trade rows sharing one
        // date, and they are one joker between them.
        return [.. played
            .GroupBy(date => date.Year)
            .Select(group => (CalendarYear: group.Key, PlayedOn: group.Min()))
            .OrderBy(use => use.CalendarYear)];
    }
}
