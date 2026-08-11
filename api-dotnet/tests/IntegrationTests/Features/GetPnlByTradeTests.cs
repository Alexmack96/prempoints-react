using Api.Domain.Entities;
using Api.Features.Trades;
using Api.Features.Trades.CreateTrades;
using Api.Infrastructure.EntityFramework;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace IntegrationTests.Features;

public class GetPnlByTradeTests : BaseIntegrationTest
{
    private const string BaseUrl = "/api/pnl/trade";

    [Fact]
    public async Task GetPnlByTrade_ShouldReturnCorrectPnl_GivenValidSetup()
    {
        var ct = TestContext.Current.CancellationToken;
        var asAtDate = new DateTime(2025, 08, 22, 12, 0, 0, DateTimeKind.Utc);
        await SeedPriceAsync("Chelsea", 80, new DateOnly(2025, 8, 15), ct);
        await SeedPriceAsync("Chelsea", 82, new DateOnly(2025, 8, 22), ct);
        await SeedPriceAsync("Arsenal", 75, new DateOnly(2025, 8, 15), ct);
        await SeedPriceAsync("Arsenal", 74, new DateOnly(2025, 8, 22), ct);

        var request = new CreateTrades.Request(
            Username: "Almack",
            TradeDateUtc: asAtDate,
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
