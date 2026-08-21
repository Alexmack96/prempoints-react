using Api.Domain.Entities;
using Api.Features.Trades;
using Api.Features.Trades.CreateTrades;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;

namespace IntegrationTests.Features;

/// <summary>
/// GET /api/v1/trades/joker-allowance — what the board asks before offering the
/// checkbox.
/// </summary>
public class GetJokerAllowanceTests : BaseIntegrationTest
{
    private const string Url = "/api/v1/trades/joker-allowance";

    private static DateTime Utc(int year, int month, int day) => new(year, month, day, 12, 0, 0, DateTimeKind.Utc);

    private Task SeedPriceAsync(string teamName, DateOnly valueDate) => WithDbAsync(async db =>
    {
        var period = await db.SeasonPeriods
            .SingleAsync(sp => sp.PeriodStartDate <= valueDate && valueDate <= sp.PeriodEndDate);

        db.Prices.Add(new PriceEntity
        {
            Id = Guid.CreateVersion7(),
            ValueDate = valueDate,
            Team = await db.Teams.SingleAsync(t => t.TeamName == teamName),
            Bid = 79.5m,
            Ask = 80.5m,
            SeasonPeriod = period,
        });

        await db.SaveChangesAsync();
    });

    private async Task PlayJokerAsync(DateTime tradeDate)
    {
        await SeedPriceAsync("Chelsea", DateOnly.FromDateTime(tradeDate));

        await AsAdmin().PostAsJsonAsync(
            "/api/v1/trades",
            new CreateTrades.Request(
                TradeDateUtc: tradeDate,
                TradeType: TradeType.Joker,
                TimezoneIana: "Europe/London",
                ExposuresByTeam: new Dictionary<string, int> { { "Chelsea", 40 } }),
            TestContext.Current.CancellationToken);
    }

    private Task<HttpResponseMessage> AllowanceAsync(DateTime tradeDate) =>
        AsAdmin().GetAsync(
            new Uri($"{Url}?tradeDateUtc={tradeDate:o}", UriKind.Relative),
            TestContext.Current.CancellationToken);

    [Fact]
    public async Task ReportsAvailable_WhenNoJokerHasBeenPlayed()
    {
        var response = await AllowanceAsync(Utc(2025, 11, 10));

        await VerifyResponse(response);
    }

    [Fact]
    public async Task ReportsUnavailable_WhenAlreadyPlayedThatCalendarYear()
    {
        await PlayJokerAsync(Utc(2025, 11, 10));

        var response = await AllowanceAsync(Utc(2025, 12, 8));

        await VerifyResponse(response);
    }

    [Fact]
    public async Task ReportsAvailableAgain_AfterNewYearInTheSameSeason()
    {
        await PlayJokerAsync(Utc(2025, 11, 10));

        // Same season, next calendar year — the allowance resets.
        var response = await AllowanceAsync(Utc(2026, 1, 5));

        await VerifyResponse(response);
    }

    [Fact]
    public async Task StaysAvailableOnTheDateTheJokerWasPlayed()
    {
        var tradeDate = Utc(2025, 11, 10);
        await PlayJokerAsync(tradeDate);

        // Editing that submission is not spending a second joker, so the board
        // must keep the box ticked rather than greying out the player's own
        // choice.
        var response = await AllowanceAsync(tradeDate);

        await VerifyResponse(response);
    }

    [Fact]
    public async Task ReturnsUnauthorized_WhenAnonymous()
    {
        var response = await AsAnonymous().GetAsync(
            new Uri(Url, UriKind.Relative),
            TestContext.Current.CancellationToken);

        await VerifyResponse(response);
    }
}
