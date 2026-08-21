using Microsoft.EntityFrameworkCore;

namespace IntegrationTests.Features;

/// GET /api/v1/teams/{id} — the canonical item route.
public class GetTeamByIdTests : BaseIntegrationTest
{
    private const string BaseUrl = "/api/v1/teams";

    [Fact]
    public async Task ReturnsTeam_WhenItExists()
    {
        var arsenalId = await WithDbAsync(db =>
            db.Teams.Where(t => t.TeamName == "Arsenal").Select(t => t.Id).SingleAsync());

        var response = await HttpClient.GetAsync(
            new Uri($"{BaseUrl}/{arsenalId}", UriKind.Relative),
            TestContext.Current.CancellationToken);

        await VerifyResponse(response);
    }

    [Fact]
    public async Task ReturnsNotFound_WhenIdIsUnknown()
    {
        var response = await HttpClient.GetAsync(
            new Uri($"{BaseUrl}/{Guid.Empty}", UriKind.Relative),
            TestContext.Current.CancellationToken);

        await VerifyResponse(response);
    }

    [Fact]
    public async Task ReturnsNotFound_WhenIdIsNotAGuid()
    {
        // The :guid route constraint means a name never reaches the handler.
        // This is what stops /teams/{teamName} and /teams/{id} from colliding.
        var response = await HttpClient.GetAsync(
            new Uri($"{BaseUrl}/Arsenal", UriKind.Relative),
            TestContext.Current.CancellationToken);

        await VerifyResponse(response);
    }
}
