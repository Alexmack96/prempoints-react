using Api.Features.Prices;
using Api.Features.Prices.CreatePrice;
using System.Net;
using System.Net.Http.Json;

namespace IntegrationTests.Features;

public class CreatePriceTests : BaseIntegrationTest
{

    [Fact]
    public async Task CreatePrice_ReturnsOk_AndCreatesPrice()
    {
        var ct = TestContext.Current.CancellationToken;
        var teamName = "Chelsea";
        var valueDate = new DateOnly(2025, 08, 15);
        var request = new CreatePrice.Request(teamName, Bid: 39.5m, Ask: 40.5m, valueDate);

        var response = await AsAdmin().PostAsJsonAsync("/api/v1/prices", request, ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PriceDto>(ct);

        Assert.NotNull(result);
        Assert.Equal(39.5m, result.Bid);
        Assert.Equal(40.5m, result.Ask);
        // Computed by SQL Server from the spread, not sent in by the caller.
        Assert.Equal(40m, result.Mid);
    }

    [Fact]
    public async Task CreatePrice_ReturnsNotFound_GivenMissingTeam()
    {
        var ct = TestContext.Current.CancellationToken;
        var teamName = "NOT_Chelsea";
        var valueDate = new DateOnly(2025, 08, 15);
        var request = new CreatePrice.Request(teamName, Bid: 39.5m, Ask: 40.5m, valueDate);

        var response = await AsAdmin().PostAsJsonAsync("/api/v1/prices", request, ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

}
