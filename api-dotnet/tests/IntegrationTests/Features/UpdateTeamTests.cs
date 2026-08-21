using Api.Features.Teams.UpdateTeam;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;

namespace IntegrationTests.Features;

/// PUT /api/v1/teams/{id} — full replacement, admin-only.
public class UpdateTeamTests : BaseIntegrationTest
{
    private const string BaseUrl = "/api/v1/teams";

    private Task<Guid> TeamIdAsync(string teamName) => WithDbAsync(db =>
        db.Teams.Where(t => t.TeamName == teamName).Select(t => t.Id).SingleAsync());

    [Fact]
    public async Task ReturnsUpdatedTeam()
    {
        var id = await TeamIdAsync("Arsenal");

        var response = await AsAdmin().PutAsJsonAsync(
            $"{BaseUrl}/{id}",
            new UpdateTeam.Request("Arsenal FC"),
            TestContext.Current.CancellationToken);

        await VerifyResponse(response);
    }

    [Fact]
    public async Task IsIdempotent_WhenNameIsUnchanged()
    {
        // The uniqueness check has to exclude the row being updated, or a PUT
        // that changes nothing 409s against itself and PUT stops being
        // idempotent.
        var id = await TeamIdAsync("Arsenal");

        var response = await AsAdmin().PutAsJsonAsync(
            $"{BaseUrl}/{id}",
            new UpdateTeam.Request("Arsenal"),
            TestContext.Current.CancellationToken);

        await VerifyResponse(response);
    }

    [Fact]
    public async Task ReturnsConflict_WhenNameBelongsToAnotherTeam()
    {
        var id = await TeamIdAsync("Arsenal");

        var response = await AsAdmin().PutAsJsonAsync(
            $"{BaseUrl}/{id}",
            new UpdateTeam.Request("Chelsea"),
            TestContext.Current.CancellationToken);

        await VerifyResponse(response);
    }

    [Fact]
    public async Task ReturnsNotFound_WhenIdIsUnknown()
    {
        var response = await AsAdmin().PutAsJsonAsync(
            $"{BaseUrl}/{Guid.Empty}",
            new UpdateTeam.Request("Ghost FC"),
            TestContext.Current.CancellationToken);

        await VerifyResponse(response);
    }

    [Fact]
    public async Task ReturnsUnprocessable_WhenNameIsEmpty()
    {
        var id = await TeamIdAsync("Arsenal");

        var response = await AsAdmin().PutAsJsonAsync(
            $"{BaseUrl}/{id}",
            new UpdateTeam.Request(""),
            TestContext.Current.CancellationToken);

        await VerifyResponse(response);
    }

    [Fact]
    public async Task ReturnsForbidden_WhenCallerIsNotAdmin()
    {
        var id = await TeamIdAsync("Arsenal");

        var response = await AsStandardUser().PutAsJsonAsync(
            $"{BaseUrl}/{id}",
            new UpdateTeam.Request("Arsenal FC"),
            TestContext.Current.CancellationToken);

        await VerifyResponse(response);
    }
}
