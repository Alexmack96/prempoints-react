using Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IntegrationTests.Features;

/// <summary>
/// DELETE /api/v1/teams/{id} — a real delete, admin-only, refused with 409
/// while anything still references the team.
/// </summary>
public class DeleteTeamTests : BaseIntegrationTest
{
    private const string BaseUrl = "/api/v1/teams";

    /// A team with no season membership, price or trade — the only kind that
    /// can actually be deleted. Every seeded team is enrolled in 2025/26.
    private async Task<Guid> UnreferencedTeamAsync()
    {
        var id = TestIds.Team(999);

        await WithDbAsync(async db =>
        {
            db.Teams.Add(new TeamEntity { Id = id, TeamName = "Typo FC" });
            await db.SaveChangesAsync();
        });

        return id;
    }

    private Task<Guid> TeamIdAsync(string teamName) => WithDbAsync(db =>
        db.Teams.Where(t => t.TeamName == teamName).Select(t => t.Id).SingleAsync());

    [Fact]
    public async Task ReturnsNoContent_WhenTeamIsUnreferenced()
    {
        var id = await UnreferencedTeamAsync();

        var response = await AsAdmin().DeleteAsync(
            new Uri($"{BaseUrl}/{id}", UriKind.Relative),
            TestContext.Current.CancellationToken);

        await VerifyResponse(response);
    }

    [Fact]
    public async Task RemovesTheTeam_WhenUnreferenced()
    {
        var id = await UnreferencedTeamAsync();

        await AsAdmin().DeleteAsync(
            new Uri($"{BaseUrl}/{id}", UriKind.Relative),
            TestContext.Current.CancellationToken);

        var exists = await WithDbAsync(db => db.Teams.AnyAsync(t => t.Id == id));

        Assert.False(exists);
    }

    [Fact]
    public async Task ReturnsConflict_WhenTeamIsEnrolledInASeason()
    {
        // Every foreign key into Teams is Restrict. Without the pre-check in
        // the handler this is a raw FK violation and the caller gets a 500 that
        // never says which relationship blocked the delete.
        var id = await TeamIdAsync("Arsenal");

        var response = await AsAdmin().DeleteAsync(
            new Uri($"{BaseUrl}/{id}", UriKind.Relative),
            TestContext.Current.CancellationToken);

        await VerifyResponse(response);
    }

    [Fact]
    public async Task ReturnsNotFound_WhenIdIsUnknown()
    {
        var response = await AsAdmin().DeleteAsync(
            new Uri($"{BaseUrl}/{Guid.Empty}", UriKind.Relative),
            TestContext.Current.CancellationToken);

        await VerifyResponse(response);
    }

    [Fact]
    public async Task ReturnsUnauthorized_WhenAnonymous()
    {
        var id = await UnreferencedTeamAsync();

        var response = await AsAnonymous().DeleteAsync(
            new Uri($"{BaseUrl}/{id}", UriKind.Relative),
            TestContext.Current.CancellationToken);

        await VerifyResponse(response);
    }

    [Fact]
    public async Task ReturnsForbidden_WhenCallerIsNotAdmin()
    {
        var id = await UnreferencedTeamAsync();

        var response = await AsStandardUser().DeleteAsync(
            new Uri($"{BaseUrl}/{id}", UriKind.Relative),
            TestContext.Current.CancellationToken);

        await VerifyResponse(response);
    }
}
