using Api.Features.Prices.CreatePrices;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;

namespace IntegrationTests.Features;

/// <summary>
/// POST /api/v1/prices/bulk — how a gameweek's price board gets loaded.
/// </summary>
public class CreatePricesTests : BaseIntegrationTest
{
    private const string Url = "/api/v1/prices/bulk";

    /// Inside gameweek 1 of the seeded 2025/26 season.
    private static readonly DateOnly ValueDate = new(2025, 8, 15);

    private static CreatePrices.Request Request(params CreatePrices.Spread[] prices) =>
        new(ValueDate, [.. prices]);

    [Fact]
    public async Task LoadsEveryPriceAndComputesTheMid()
    {
        var response = await AsAdmin().PostAsJsonAsync(
            Url,
            Request(
                new CreatePrices.Spread("Arsenal", 80.5m, 82m),
                new CreatePrices.Spread("Chelsea", 68.5m, 70m),
                new CreatePrices.Spread("Hull", 23m, 24.5m)),
            TestContext.Current.CancellationToken);

        await VerifyResponse(response);
    }

    [Fact]
    public async Task PersistsTheSpreadAndLetsSqlDeriveTheMid()
    {
        await AsAdmin().PostAsJsonAsync(
            Url,
            Request(new CreatePrices.Spread("Arsenal", 80.5m, 82m)),
            TestContext.Current.CancellationToken);

        var price = await WithDbAsync(db => db.Prices
            .Where(p => p.Team.TeamName == "Arsenal" && p.ValueDate == ValueDate)
            .SingleAsync());

        Assert.Equal(80.5m, price.Bid);
        Assert.Equal(82m, price.Ask);
        // Never written by the handler — this is the database's answer.
        Assert.Equal(81.25m, price.Mid);
    }

    [Fact]
    public async Task UpsertsRatherThanFailingOnReload()
    {
        var ct = TestContext.Current.CancellationToken;

        await AsAdmin().PostAsJsonAsync(Url, Request(new CreatePrices.Spread("Arsenal", 80.5m, 82m)), ct);

        // Re-loading a board with corrected numbers must fix it, not collide on
        // the unique index over team and value date.
        await AsAdmin().PostAsJsonAsync(Url, Request(new CreatePrices.Spread("Arsenal", 79m, 80m)), ct);

        var prices = await WithDbAsync(db => db.Prices
            .Where(p => p.Team.TeamName == "Arsenal" && p.ValueDate == ValueDate)
            .ToListAsync());

        Assert.Single(prices);
        Assert.Equal(79.5m, prices[0].Mid);
    }

    [Fact]
    public async Task WritesNothing_WhenAnyTeamIsUnknown()
    {
        var ct = TestContext.Current.CancellationToken;

        var response = await AsAdmin().PostAsJsonAsync(
            Url,
            Request(new CreatePrices.Spread("Arsenal", 80.5m, 82m), new CreatePrices.Spread("Wrexham", 20m, 21m)),
            ct);

        var arsenalPrices = await WithDbAsync(db => db.Prices
            .CountAsync(p => p.Team.TeamName == "Arsenal" && p.ValueDate == ValueDate));

        // A partial board is worse than none: some clubs tradeable, the rest
        // silently not, and nobody finds out until someone tries to back one.
        Assert.Equal(0, arsenalPrices);
        await VerifyResponse(response);
    }

    [Fact]
    public async Task ReturnsNotFound_WhenTheDateIsOutsideEverySeason()
    {
        var response = await AsAdmin().PostAsJsonAsync(
            Url,
            new CreatePrices.Request(new DateOnly(2030, 1, 1), [new CreatePrices.Spread("Arsenal", 80.5m, 82m)]),
            TestContext.Current.CancellationToken);

        await VerifyResponse(response);
    }

    [Fact]
    public async Task ReturnsUnprocessable_WhenASpreadIsInverted()
    {
        var response = await AsAdmin().PostAsJsonAsync(
            Url,
            Request(new CreatePrices.Spread("Arsenal", 82m, 80.5m)),
            TestContext.Current.CancellationToken);

        await VerifyResponse(response);
    }

    [Fact]
    public async Task ReturnsForbidden_WhenCallerIsNotAdmin()
    {
        var response = await AsStandardUser().PostAsJsonAsync(
            Url,
            Request(new CreatePrices.Spread("Arsenal", 80.5m, 82m)),
            TestContext.Current.CancellationToken);

        await VerifyResponse(response);
    }
}
