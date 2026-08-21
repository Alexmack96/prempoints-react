using Api.Domain.Entities;
using Api.Features.Trades;
using Api.Features.Trades.CreateTrades;
using Api.Infrastructure.EntityFramework;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;

namespace IntegrationTests.Features;

/// <summary>
/// GET /api/v1/leaderboard — the season standings.
/// </summary>
public class GetLeaderboardTests : BaseIntegrationTest
{
    private const string Url = "/api/v1/leaderboard?asAtDate=2025-08-15";

    private static Uri Relative(string url) => new(url, UriKind.Relative);

    [Fact]
    public async Task ListsEveryEnrolledPlayerBeforeAnyoneHasTraded()
    {
        // Day one. Nobody has traded, so everybody is on zero and joint first —
        // which is exactly when a leaderboard that only listed players with a
        // score would come back empty and look broken.
        var response = await HttpClient.GetAsync(Relative(Url), TestContext.Current.CancellationToken);

        await VerifyResponse(response);
    }

    [Fact]
    public async Task CountsTradesPlacedWithoutScoringThemYet()
    {
        var ct = TestContext.Current.CancellationToken;

        await SeedPriceAsync("Chelsea", 80m, new DateOnly(2025, 8, 15), ct);
        await SeedPriceAsync("Arsenal", 79m, new DateOnly(2025, 8, 15), ct);

        await AsAdmin().PostAsJsonAsync(
            "/api/v1/trades",
            new CreateTrades.Request(
                TradeDateUtc: new DateTime(2025, 8, 15, 12, 0, 0, DateTimeKind.Utc),
                TradeType: TradeType.Standard,
                TimezoneIana: "Europe/London",
                ExposuresByTeam: new Dictionary<string, int>(StringComparer.Ordinal)
                {
                    ["Chelsea"] = 20,
                    ["Arsenal"] = 20,
                }),
            ct);

        // The trade count is real; the PnL is not scored yet, so the player who
        // traded is still level with everyone else. PnlIsSettled says so rather
        // than leaving a client to read the zero as a result.
        var response = await HttpClient.GetAsync(Relative(Url), ct);

        await VerifyResponse(response);
    }

    [Fact]
    public async Task ReturnsNotFound_WhenTheDateIsOutsideEverySeason()
    {
        var response = await HttpClient.GetAsync(
            Relative("/api/v1/leaderboard?asAtDate=2027-06-01"),
            TestContext.Current.CancellationToken);

        await VerifyResponse(response);
    }

    private async Task SeedPriceAsync(string teamName, decimal priceValue, DateOnly valueDate, CancellationToken ct)
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PremPointsDbContext>();

        context.Prices.Add(new PriceEntity
        {
            Id = Guid.CreateVersion7(),
            ValueDate = valueDate,
            Team = await DataSeeder.GetTeamAsync(context, teamName),
            Bid = priceValue - 0.5m,
            Ask = priceValue + 0.5m,
            SeasonPeriod = await DataSeeder.GetSeasonPeriodAsync(context, 1, 2025),
        });

        await context.SaveChangesAsync(ct);
    }
}
