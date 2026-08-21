
using Api.Domain.Entities;
using Api.Features.Trades;
using Api.Features.Trades.CreateTrades;
using Api.Infrastructure.EntityFramework;
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
            Username: "Almack",
            TradeDateUtc: tradeDate,
            TradeType: TradeType.Standard,
            TimezoneIana: "Europe/London",
            ExposuresByTeam: new Dictionary<string, int> { { "Chelsea", 40 } }
        );

        // Act
        var response = await HttpClient.PostAsJsonAsync(BaseUrl, request, ct);

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
            Username: "Almack",
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
        var response = await HttpClient.PostAsJsonAsync(BaseUrl, request, ct);

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
            Username: "GHOSTUSER",
            TradeDateUtc: tradeDate,
            TradeType: TradeType.Standard,
            TimezoneIana: "Europe/London",
            ExposuresByTeam: new Dictionary<string, int> { { "Chelsea", 10 } }
        );

        // Act
        var response = await HttpClient.PostAsJsonAsync(BaseUrl, request, ct);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
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
            Price = priceValue,
            SeasonPeriod = await DataSeeder.GetSeasonPeriodAsync(context, 1, 2025)
        };

        context.Prices.Add(price);
        await context.SaveChangesAsync(ct);
    }
}