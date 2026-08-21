
using Api.Domain.Entities;
using Api.Features.Trades;
using Api.Features.Trades.CreateTrades;
using Api.Infrastructure.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace IntegrationTests.Features;

public class CreateTradesTests : BaseIntegrationTest
{
    private const string BaseUrl = "/api/v1/trades";

    [Fact]
    public async Task CreateTrades_ReturnsOk_AndCreatesSingleTrade()
    {
        var ct = TestContext.Current.CancellationToken;
        var tradeDate = new DateTime(2025, 08, 15, 12, 0, 0, DateTimeKind.Utc);

        // Arrange
        await SeedPriceAsync("Chelsea", 80, new DateOnly(2025, 8, 15), ct);

        var request = new CreateTrades.Request(
            TradeDateUtc: tradeDate,
            TradeType: TradeType.Standard,
            TimezoneIana: "Europe/London",
            ExposuresByTeam: new Dictionary<string, int> { { "Chelsea", 40 } }
        );

        // Act
        var response = await AsAdmin().PostAsJsonAsync(BaseUrl, request, ct);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<List<TradeDto>>(ct);

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(40, result[0].Exposure);
    }

    [Fact]
    public async Task CreateTrades_WithMultipleTeams_ReturnsOk_AndCreatesAll()
    {
        var ct = TestContext.Current.CancellationToken;
        var tradeDate = new DateTime(2025, 08, 15, 12, 0, 0, DateTimeKind.Utc);

        // Arrange
        await SeedPriceAsync("Chelsea", 80, new DateOnly(2025, 8, 15), ct);
        await SeedPriceAsync("Arsenal", 79, new DateOnly(2025, 8, 15), ct);

        var request = new CreateTrades.Request(
            TradeDateUtc: tradeDate,
            TradeType: TradeType.Standard,
            TimezoneIana: "Europe/London",
            ExposuresByTeam: new Dictionary<string, int>
            {
                { "Chelsea", 15 },
                { "Arsenal", 25 }
            }
        );

        // Act
        var response = await AsAdmin().PostAsJsonAsync(BaseUrl, request, ct);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<List<TradeDto>>(ct);

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);

        // verify specific data points
        Assert.Contains(result, t => t.Exposure == 15);
        Assert.Contains(result, t => t.Exposure == 25);
    }

    [Fact]
    public async Task CreateTrades_WithNonExistentUser_ReturnsNotFound()
    {
        var ct = TestContext.Current.CancellationToken;
        var tradeDate = new DateTime(2025, 08, 15, 12, 0, 0, DateTimeKind.Utc);

        // Arrange
        await SeedPriceAsync("Chelsea", 80, new DateOnly(2025, 8, 15), ct);

        var request = new CreateTrades.Request(
            TradeDateUtc: tradeDate,
            TradeType: TradeType.Standard,
            TimezoneIana: "Europe/London",
            ExposuresByTeam: new Dictionary<string, int> { { "Chelsea", 40 } }
        );

        // Act
        var response = await AsUser("user_ghost").PostAsJsonAsync(BaseUrl, request, ct);

        // Assert: authenticated with WorkOS, but no PremPoints account.
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ReturnsUnprocessable_WhenStakesDoNotTotalForty()
    {
        var ct = TestContext.Current.CancellationToken;
        await SeedPriceAsync("Chelsea", 80, new DateOnly(2025, 8, 15), ct);

        var request = new CreateTrades.Request(
            TradeDateUtc: new DateTime(2025, 08, 15, 12, 0, 0, DateTimeKind.Utc),
            TradeType: TradeType.Standard,
            TimezoneIana: "Europe/London",
            ExposuresByTeam: new Dictionary<string, int> { { "Chelsea", 20 } }
        );

        var response = await AsAdmin().PostAsJsonAsync(BaseUrl, request, ct);

        await VerifyResponse(response);
    }

    [Fact]
    public async Task ReturnsUnprocessable_WhenAStakeIsNotAMultipleOfFive()
    {
        var ct = TestContext.Current.CancellationToken;
        await SeedPriceAsync("Chelsea", 80, new DateOnly(2025, 8, 15), ct);
        await SeedPriceAsync("Arsenal", 79, new DateOnly(2025, 8, 15), ct);

        // Totals forty, but neither stake is a legal size.
        var request = new CreateTrades.Request(
            TradeDateUtc: new DateTime(2025, 08, 15, 12, 0, 0, DateTimeKind.Utc),
            TradeType: TradeType.Standard,
            TimezoneIana: "Europe/London",
            ExposuresByTeam: new Dictionary<string, int> { { "Chelsea", 23 }, { "Arsenal", 17 } }
        );

        var response = await AsAdmin().PostAsJsonAsync(BaseUrl, request, ct);

        await VerifyResponse(response);
    }

    [Fact]
    public async Task ReturnsUnprocessable_WhenMoreThanTwoClubsAreBacked()
    {
        var ct = TestContext.Current.CancellationToken;
        await SeedPriceAsync("Chelsea", 80, new DateOnly(2025, 8, 15), ct);
        await SeedPriceAsync("Arsenal", 79, new DateOnly(2025, 8, 15), ct);
        await SeedPriceAsync("Everton", 45, new DateOnly(2025, 8, 15), ct);

        var request = new CreateTrades.Request(
            TradeDateUtc: new DateTime(2025, 08, 15, 12, 0, 0, DateTimeKind.Utc),
            TradeType: TradeType.Standard,
            TimezoneIana: "Europe/London",
            ExposuresByTeam: new Dictionary<string, int>
            {
                { "Chelsea", 20 }, { "Arsenal", 15 }, { "Everton", 5 }
            }
        );

        var response = await AsAdmin().PostAsJsonAsync(BaseUrl, request, ct);

        await VerifyResponse(response);
    }

    [Fact]
    public async Task CountsAShortTowardsTheTotal()
    {
        var ct = TestContext.Current.CancellationToken;
        await SeedPriceAsync("Chelsea", 80, new DateOnly(2025, 8, 15), ct);
        await SeedPriceAsync("Arsenal", 79, new DateOnly(2025, 8, 15), ct);

        // A short is a position of the same size as a long, so the total is
        // taken on absolute values: 20 short plus 20 long is a full forty.
        var request = new CreateTrades.Request(
            TradeDateUtc: new DateTime(2025, 08, 15, 12, 0, 0, DateTimeKind.Utc),
            TradeType: TradeType.Standard,
            TimezoneIana: "Europe/London",
            ExposuresByTeam: new Dictionary<string, int> { { "Chelsea", -20 }, { "Arsenal", 20 } }
        );

        var response = await AsAdmin().PostAsJsonAsync(BaseUrl, request, ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PersistsTheJoker()
    {
        var ct = TestContext.Current.CancellationToken;
        await SeedPriceAsync("Chelsea", 80, new DateOnly(2025, 8, 15), ct);

        var request = new CreateTrades.Request(
            TradeDateUtc: new DateTime(2025, 08, 15, 12, 0, 0, DateTimeKind.Utc),
            TradeType: TradeType.Joker,
            TimezoneIana: "Europe/London",
            ExposuresByTeam: new Dictionary<string, int> { { "Chelsea", 40 } }
        );

        await AsAdmin().PostAsJsonAsync(BaseUrl, request, ct);

        var stored = await WithDbAsync(db => db.Trades
            .Where(t => t.Team.TeamName == "Chelsea")
            .Select(t => t.TradeType)
            .SingleAsync());

        // The PnL multiplier reads this column, so it has to survive the write.
        Assert.Equal(TradeType.Joker, stored);
    }

    [Fact]
    public async Task UpdatesTheJokerWhenTradesAreResubmitted()
    {
        var ct = TestContext.Current.CancellationToken;
        var tradeDate = new DateTime(2025, 08, 15, 12, 0, 0, DateTimeKind.Utc);
        await SeedPriceAsync("Chelsea", 80, new DateOnly(2025, 8, 15), ct);

        CreateTrades.Request Request(TradeType tradeType) => new(
            TradeDateUtc: tradeDate,
            TradeType: tradeType,
            TimezoneIana: "Europe/London",
            ExposuresByTeam: new Dictionary<string, int> { { "Chelsea", 40 } }
        );

        await AsAdmin().PostAsJsonAsync(BaseUrl, Request(TradeType.Standard), ct);
        await AsAdmin().PostAsJsonAsync(BaseUrl, Request(TradeType.Joker), ct);

        var stored = await WithDbAsync(db => db.Trades
            .Where(t => t.Team.TeamName == "Chelsea")
            .Select(t => t.TradeType)
            .SingleAsync());

        // Resubmitting updates the existing row rather than inserting, and that
        // path used to leave TradeType alone — so playing the joker on a second
        // pass silently did nothing.
        Assert.Equal(TradeType.Joker, stored);
    }

    private async Task SeedPriceAsync(string teamName, decimal priceValue, DateOnly valueDate, CancellationToken ct)
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PremPointsDbContext>();

        var price = new PriceEntity
        {
            Id = Guid.CreateVersion7(),
            ValueDate = valueDate,
            Team = await DataSeeder.GetTeamAsync(context, teamName),
            // Straddled so the computed Mid lands exactly on priceValue, which
            // is what the assertions below are written against.
            Bid = priceValue - 0.5m,
            Ask = priceValue + 0.5m,
            SeasonPeriod = await DataSeeder.GetSeasonPeriodAsync(context, 1, 2025)
        };

        context.Prices.Add(price);
        await context.SaveChangesAsync(ct);
    }
}