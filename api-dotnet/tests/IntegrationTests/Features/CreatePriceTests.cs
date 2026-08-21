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
        var price = 40;
        var valueDate = new DateOnly(2025, 08, 15);
        var request = new CreatePrice.Request(teamName, price, valueDate);

        var response = await AsAdmin().PostAsJsonAsync("/api/v1/prices", request, ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PriceDto>(ct);

        Assert.NotNull(result);
        Assert.Equal(40, result.Price);
    }

    [Fact]
    public async Task CreatePrice_ReturnsNotFound_GivenMissingTeam()
    {
        var ct = TestContext.Current.CancellationToken;
        var teamName = "NOT_Chelsea";
        var price = 40;
        var valueDate = new DateOnly(2025, 08, 15);
        var request = new CreatePrice.Request(teamName, price, valueDate);

        var response = await AsAdmin().PostAsJsonAsync("/api/v1/prices", request, ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

}
