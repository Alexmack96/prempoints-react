using Api.Features.Prices.CreatePrices;
using System.Net.Http.Json;

namespace IntegrationTests.Features;

/// <summary>
/// GET /api/v1/prices/summary — the price board behind the Prices page.
/// </summary>
public class GetPriceSummaryTests : BaseIntegrationTest
{
    private const string Url = "/api/v1/prices/summary?pageSize=100";
    private const string BulkUrl = "/api/v1/prices/bulk";

    private Task LoadAsync(DateOnly valueDate, params CreatePrices.Spread[] prices) =>
        AsAdmin().PostAsJsonAsync(
            BulkUrl,
            new CreatePrices.Request(valueDate, [.. prices]),
            TestContext.Current.CancellationToken);

    [Fact]
    public async Task ListsEveryClubEvenWithoutAPrice()
    {
        // No prices loaded at all. Every club still appears, because a club
        // silently missing from the board is how a gap goes unnoticed.
        var response = await HttpClient.GetAsync(
            new Uri(Url, UriKind.Relative),
            TestContext.Current.CancellationToken);

        await VerifyResponse(response);
    }

    [Fact]
    public async Task ReportsMovementBetweenTheLastTwoPrices()
    {
        await LoadAsync(
            new DateOnly(2025, 8, 15),
            new CreatePrices.Spread("Arsenal", 80m, 82m),
            new CreatePrices.Spread("Chelsea", 68m, 70m),
            new CreatePrices.Spread("Everton", 44m, 46m));

        await LoadAsync(
            new DateOnly(2025, 8, 22),
            // Arsenal up, Chelsea down, Everton unchanged.
            new CreatePrices.Spread("Arsenal", 84m, 86m),
            new CreatePrices.Spread("Chelsea", 64m, 66m),
            new CreatePrices.Spread("Everton", 44m, 46m));

        var response = await HttpClient.GetAsync(
            new Uri("/api/v1/prices/summary?pageSize=5", UriKind.Relative),
            TestContext.Current.CancellationToken);

        await VerifyResponse(response);
    }

    [Fact]
    public async Task IgnoresPricesAfterTheRequestedDate()
    {
        await LoadAsync(new DateOnly(2025, 8, 15), new CreatePrices.Spread("Arsenal", 80m, 82m));
        await LoadAsync(new DateOnly(2025, 8, 22), new CreatePrices.Spread("Arsenal", 84m, 86m));

        // Asking as at the 15th must not see the 22nd's price, or the board
        // would show a quote nobody could have traded on that day.
        var response = await HttpClient.GetAsync(
            new Uri("/api/v1/prices/summary?asAtDate=2025-08-15&pageSize=1", UriKind.Relative),
            TestContext.Current.CancellationToken);

        await VerifyResponse(response);
    }
}
