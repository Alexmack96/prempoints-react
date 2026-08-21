using Api.Domain.Entities;
using Api.Features.Trades;
using Api.Features.Trades.CreateTrades;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Json;

namespace IntegrationTests.Features;

/// <summary>
/// One joker per calendar year within a season.
/// <para>
/// A season straddles New Year, so the allowance works out at two per season.
/// Scoping it to the season as well as the year is what lets two jokers fall in
/// the same calendar year when they belong to different seasons.
/// </para>
/// <para>
/// These do not use CreateTradesTests' price helper: that pins every price to
/// gameweek 1 of 2025, so every trade would land in the same season and the
/// rule under test could never be exercised.
/// </para>
/// </summary>
public class JokerAllowanceTests : BaseIntegrationTest
{
    private const string Url = "/api/v1/trades";

    /// A price on the gameweek that actually covers the date, so the resulting
    /// trade belongs to the right season.
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

    private async Task<HttpResponseMessage> PlayJokerAsync(DateTime tradeDate)
    {
        await SeedPriceAsync("Chelsea", DateOnly.FromDateTime(tradeDate));

        return await AsAdmin().PostAsJsonAsync(
            Url,
            new CreateTrades.Request(
                TradeDateUtc: tradeDate,
                TradeType: TradeType.Joker,
                TimezoneIana: "Europe/London",
                ExposuresByTeam: new Dictionary<string, int> { { "Chelsea", 40 } }),
            TestContext.Current.CancellationToken);
    }

    private static DateTime Utc(int year, int month, int day) => new(year, month, day, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task AllowsOneJokerEitherSideOfNewYearWithinASeason()
    {
        // Both sit in the seeded 2025/26 season, in different calendar years.
        var december = await PlayJokerAsync(Utc(2025, 12, 8));
        var january = await PlayJokerAsync(Utc(2026, 1, 5));

        Assert.Equal(HttpStatusCode.OK, december.StatusCode);
        Assert.Equal(HttpStatusCode.OK, january.StatusCode);
    }

    [Fact]
    public async Task RejectsASecondJokerInTheSameCalendarYear()
    {
        await PlayJokerAsync(Utc(2025, 11, 10));

        var second = await PlayJokerAsync(Utc(2025, 12, 8));

        await VerifyResponse(second);
    }

    [Fact]
    public async Task AllowsTheSameCalendarYearInDifferentSeasons()
    {
        // A gameweek in the 2026/27 season, which the seeder creates without
        // any periods of its own.
        await WithDbAsync(async db =>
        {
            var nextSeason = await db.Seasons.SingleAsync(season => season.StartYear == 2026);

            db.SeasonPeriods.Add(new SeasonPeriodEntity
            {
                Id = Guid.CreateVersion7(),
                GameweekNumber = 1,
                PeriodStartDate = new DateOnly(2026, 11, 1),
                PeriodEndDate = new DateOnly(2026, 11, 7),
                Season = nextSeason,
            });

            await db.SaveChangesAsync();
        });

        // January 2026 is the 2025/26 season; November 2026 is 2026/27. Same
        // calendar year, so only the season scoping makes this legal.
        var january = await PlayJokerAsync(Utc(2026, 1, 5));
        var november = await PlayJokerAsync(Utc(2026, 11, 4));

        Assert.Equal(HttpStatusCode.OK, january.StatusCode);
        Assert.Equal(HttpStatusCode.OK, november.StatusCode);
    }

    [Fact]
    public async Task LetsTheSameGameweekBeResubmittedWithTheJokerStillOn()
    {
        var tradeDate = Utc(2025, 11, 10);

        await PlayJokerAsync(tradeDate);

        // The resubmission splits across two clubs, so Arsenal needs a price on
        // the same date or the edit fails for an unrelated reason.
        await SeedPriceAsync("Arsenal", DateOnly.FromDateTime(tradeDate));

        // Editing a submission is not spending a second joker, so the check has
        // to exclude the trades it is about to update.
        var again = await AsAdmin().PostAsJsonAsync(
            Url,
            new CreateTrades.Request(
                TradeDateUtc: tradeDate,
                TradeType: TradeType.Joker,
                TimezoneIana: "Europe/London",
                ExposuresByTeam: new Dictionary<string, int> { { "Chelsea", 20 }, { "Arsenal", 20 } }),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, again.StatusCode);
    }
}
