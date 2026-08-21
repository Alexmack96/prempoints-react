namespace IntegrationTests.Features;

/// <summary>
/// GET /api/v1/teams — the single read-collection for teams. "Active" and
/// "by name" are filters here rather than routes of their own.
/// </summary>
public class GetTeamsTests : BaseIntegrationTest
{
    private const string BaseUrl = "/api/v1/teams";

    [Fact]
    public async Task ReturnsFirstPage_WithPagingMetadata()
    {
        var response = await HttpClient.GetAsync(
            new Uri($"{BaseUrl}?page=1&pageSize=5", UriKind.Relative),
            TestContext.Current.CancellationToken);

        await VerifyResponse(response);
    }

    [Fact]
    public async Task FiltersToTeamsActiveOnDate()
    {
        // 15 Aug 2025 is gameweek 1 of the seeded 2025/26 season.
        var response = await HttpClient.GetAsync(
            new Uri($"{BaseUrl}?activeOn=2025-08-15&pageSize=3", UriKind.Relative),
            TestContext.Current.CancellationToken);

        await VerifyResponse(response);
    }

    [Fact]
    public async Task ReturnsEmptyPage_WhenNoSeasonCoversTheDate()
    {
        // The behaviour this pins down: a well-formed filter that matches
        // nothing is 200 with an empty page. The endpoint this replaced
        // returned CriticalError — a 500 — for exactly this case.
        var response = await HttpClient.GetAsync(
            new Uri($"{BaseUrl}?activeOn=2030-01-01", UriKind.Relative),
            TestContext.Current.CancellationToken);

        await VerifyResponse(response);
    }

    [Fact]
    public async Task FiltersByNameFragment()
    {
        var response = await HttpClient.GetAsync(
            new Uri($"{BaseUrl}?name=Man", UriKind.Relative),
            TestContext.Current.CancellationToken);

        await VerifyResponse(response);
    }

    [Fact]
    public async Task SortsByNameDescending()
    {
        var response = await HttpClient.GetAsync(
            new Uri($"{BaseUrl}?sort=-teamName&pageSize=3", UriKind.Relative),
            TestContext.Current.CancellationToken);

        await VerifyResponse(response);
    }

    [Fact]
    public async Task ReturnsRequestedPage()
    {
        var response = await HttpClient.GetAsync(
            new Uri($"{BaseUrl}?page=2&pageSize=5", UriKind.Relative),
            TestContext.Current.CancellationToken);

        await VerifyResponse(response);
    }

    [Fact]
    public async Task RejectsPageSizeAboveMaximum()
    {
        // Refused rather than clamped: a caller that asks for 500 and silently
        // receives 100 will page through the collection wrongly.
        var response = await HttpClient.GetAsync(
            new Uri($"{BaseUrl}?pageSize=500", UriKind.Relative),
            TestContext.Current.CancellationToken);

        await VerifyResponse(response);
    }

    [Fact]
    public async Task RejectsUnknownSortKey()
    {
        var response = await HttpClient.GetAsync(
            new Uri($"{BaseUrl}?sort=createdBy", UriKind.Relative),
            TestContext.Current.CancellationToken);

        await VerifyResponse(response);
    }
}
