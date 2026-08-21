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
    private const string BaseUrl = "/api/v1/pnl/trade";

    // Two separate problems, neither of which a green tick would be honest about:
    //
    // 1. This test POSTs a CreateTrades.Request to /api/v1/pnl/trade, which is a
    //    MapGet — hence the 405 it fails with. Its assertions (a List<TradeDto>
    //    with one entry) describe CreateTrades' response, not a PnL response,
    //    and CreateTradesTests already covers that endpoint.
    // 2. The endpoint it names is not finished. GetPnlByTradeHandler hardcodes
    //    PnlValue = 10 and calls GetLatestPrice, which throws
    //    NotImplementedException. Pointing this test at the real GET would swap
    //    a 405 for a 500.
    //
    // Skipped rather than deleted so the missing coverage stays visible, and
    // rather than repointed so it does not become a duplicate that hides the
    // fact that PnL has no tests because PnL has no implementation.
    [Fact(Skip = "GetPnlByTradeHandler.GetLatestPrice throws NotImplementedException; PnlValue is hardcoded.")]
    public async Task GetPnlByTrade_ShouldReturnCorrectPnl_GivenValidSetup()
    {
        var ct = TestContext.Current.CancellationToken;
        var asAtDate = new DateTime(2025, 08, 22, 12, 0, 0, DateTimeKind.Utc);
        await SeedPriceAsync("Chelsea", 80, new DateOnly(2025, 8, 15), ct);
        await SeedPriceAsync("Chelsea", 82, new DateOnly(2025, 8, 22), ct);
        await SeedPriceAsync("Arsenal", 75, new DateOnly(2025, 8, 15), ct);
        await SeedPriceAsync("Arsenal", 74, new DateOnly(2025, 8, 22), ct);

        var request = new CreateTrades.Request(
            TradeDateUtc: asAtDate,
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
