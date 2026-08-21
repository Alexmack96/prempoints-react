using Api.Features.Teams.CreateTeam;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;

namespace IntegrationTests.Features;

/// <summary>
/// Who wrote a row, as recorded by <c>AuditableEntityInterceptor</c>.
/// <para>
/// Every audit column in the live database read
/// <c>00000000-0000-0000-0000-000000000000</c>, which is what the interceptor
/// leaves behind when no caller is signed in. That was not a broken
/// interceptor — it was rows written through endpoints that required no token,
/// which is now closed. These pin the working half so a regression in the claim
/// pipeline shows up here rather than as a table of empty guids six months on.
/// </para>
/// </summary>
public class AuditingTests : BaseIntegrationTest
{
    [Fact]
    public async Task RecordsTheSignedInAdminAsTheAuthorOfARowTheyCreate()
    {
        var ct = TestContext.Current.CancellationToken;

        await AsAdmin().PostAsJsonAsync("/api/v1/teams", new CreateTeam.Request("AlexFC"), ct);

        var team = await WithDbAsync(db => db.Teams.SingleAsync(t => t.TeamName == "AlexFC"));

        // The seeded administrator, projected onto the principal as the
        // InternalUserId claim and read back out by CurrentUserService.
        Assert.Equal(TestIds.User(1), team.CreatedBy);
        Assert.Equal(TestIds.User(1), team.LastModifiedBy);
    }

    [Fact]
    public async Task RecordsTheEditorOnAnUpdateWithoutRewritingWhoCreatedIt()
    {
        var ct = TestContext.Current.CancellationToken;

        await AsAdmin().PostAsJsonAsync("/api/v1/teams", new CreateTeam.Request("AlexFC"), ct);
        var created = await WithDbAsync(db => db.Teams.SingleAsync(t => t.TeamName == "AlexFC"));

        await AsAdmin().PutAsJsonAsync(
            $"/api/v1/teams/{created.Id}",
            new { TeamName = "AlexFC Reserves" },
            ct);

        var updated = await WithDbAsync(db => db.Teams.SingleAsync(t => t.Id == created.Id));

        // Creation details survive an edit — the interceptor marks them
        // unmodified on purpose, so an update cannot quietly reassign authorship.
        Assert.Equal(TestIds.User(1), updated.CreatedBy);
        Assert.Equal(created.CreatedAtUtc, updated.CreatedAtUtc);
        Assert.Equal(TestIds.User(1), updated.LastModifiedBy);
    }
}
