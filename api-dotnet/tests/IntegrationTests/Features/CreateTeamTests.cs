using Api.Domain.Entities;
using Api.Features.Teams.CreateTeam;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;

namespace IntegrationTests.Features;

/// POST /api/v1/teams — admin-only, 201 with a Location header.
public class CreateTeamTests : BaseIntegrationTest
{
    private const string BaseUrl = "/api/v1/teams";

    [Fact]
    public async Task ReturnsCreated_WithLocationHeader()
    {
        var response = await AsAdmin().PostAsJsonAsync(
            BaseUrl,
            new CreateTeam.Request("AlexFC"),
            TestContext.Current.CancellationToken);

        await VerifyResponse(response);
    }

    [Fact]
    public async Task EnrolsTheTeamInTheCurrentSeason()
    {
        await AsAdmin().PostAsJsonAsync(
            BaseUrl,
            new CreateTeam.Request("AlexFC"),
            TestContext.Current.CancellationToken);

        // Asserted against the database rather than the response: the season
        // enrolment is a side effect the DTO deliberately does not expose, so
        // nothing in the snapshot would catch it regressing.
        var enrolled = await WithDbAsync(db => db.TeamSeasons
            .Include(ts => ts.Team)
            .Include(ts => ts.Season)
            .Where(ts => ts.Team.TeamName == "AlexFC")
            .Select(ts => ts.Season.SeasonName)
            .SingleOrDefaultAsync());

        Assert.Equal("PremPoints 2025/26", enrolled);
    }

    [Fact]
    public async Task ReturnsConflict_WhenNameAlreadyExists()
    {
        var response = await AsAdmin().PostAsJsonAsync(
            BaseUrl,
            new CreateTeam.Request("Arsenal"),
            TestContext.Current.CancellationToken);

        await VerifyResponse(response);
    }

    [Fact]
    public async Task ReturnsUnprocessable_WhenNameIsEmpty()
    {
        var response = await AsAdmin().PostAsJsonAsync(
            BaseUrl,
            new CreateTeam.Request(""),
            TestContext.Current.CancellationToken);

        await VerifyResponse(response);
    }

    [Fact]
    public async Task ReturnsUnauthorized_WhenAnonymous()
    {
        var response = await AsAnonymous().PostAsJsonAsync(
            BaseUrl,
            new CreateTeam.Request("AlexFC"),
            TestContext.Current.CancellationToken);

        await VerifyResponse(response);
    }

    [Fact]
    public async Task ReturnsForbidden_WhenCallerIsNotAdmin()
    {
        var response = await AsStandardUser().PostAsJsonAsync(
            BaseUrl,
            new CreateTeam.Request("AlexFC"),
            TestContext.Current.CancellationToken);

        await VerifyResponse(response);
    }

    [Fact]
    public async Task DoesNotCreateTheTeam_WhenCallerIsNotAdmin()
    {
        await AsStandardUser().PostAsJsonAsync(
            BaseUrl,
            new CreateTeam.Request("AlexFC"),
            TestContext.Current.CancellationToken);

        var exists = await WithDbAsync(db => db.Teams.AnyAsync(t => t.TeamName == "AlexFC"));

        Assert.False(exists);
    }
}
