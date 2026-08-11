using Api.Features.Teams;
using Api.Features.Teams.CreateTeam;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace IntegrationTests.Features;

public class CreateTeamTests : BaseIntegrationTest
{

    [Fact]
    public async Task CreateTeam_ReturnsOk_AndCreatesTeam()
    {
        var ct = TestContext.Current.CancellationToken;
        var teamName = "AlexFC";
        var request = new CreateTeam.Request(teamName);
        // We explicitly say "I am user_1". 
        // This matches the ID in our TestDataSeeder and will be put into ClaimTypes.NameIdentifier
        HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test", "user_1");
        var response = await HttpClient.PostAsJsonAsync("/api/teams", request, ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<TeamDto>(ct);

        Assert.NotNull(result);
        Assert.Equal("AlexFC", result.TeamName);
    }
    [Fact]
    public async Task CreateTeam_ReturnsBadRequest_WhenNameIsEmpty()
    {
        var ct = TestContext.Current.CancellationToken;
        var request = new CreateTeam.Request("");
        var response = await HttpClient.PostAsJsonAsync("/api/teams", request, ct);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

}
