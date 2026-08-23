using Api.Domain.Authorization;
using Api.Infrastructure.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;

namespace IntegrationTests.Features;

/// <summary>
/// First sign-in creates the PremPoints user row. Sign-up is invitation-only,
/// so every WorkOS identity arriving here is one an administrator sent an
/// invite to, and there is nobody left to approve afterwards.
/// </summary>
public class UserProvisioningTests : BaseIntegrationTest
{
    [Fact]
    public async Task FirstSignIn_CreatesTheUserFromTokenClaims()
    {
        var ct = TestContext.Current.CancellationToken;

        var response = await AsNewUser(
            "user_new_1",
            email: "ruud.vannistelrooy@example.com",
            firstName: "Ruud",
            lastName: "van Nistelrooy").GetAsync("/api/v1/users/me", ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PremPointsDbContext>();

        var created = await context.Users
            .SingleOrDefaultAsync(u => u.WorkOSUserId == "user_new_1", ct);

        Assert.NotNull(created);
        Assert.Equal("Ruud", created.FirstName);
        Assert.Equal("van Nistelrooy", created.LastName);

        // Derived from the name, not the address, because the username is shown
        // on the leaderboard and an email-derived one would publish the local
        // part of everyone's address. Stripped to letters and digits.
        Assert.Equal("RuudvanNistelrooy", created.Username);

        // Never Administrator. Signing in is not a way to become one.
        Assert.Equal(UserRole.Standard, created.Role);
    }

    [Fact]
    public async Task SecondSignIn_ReusesTheSameRow()
    {
        var ct = TestContext.Current.CancellationToken;

        await AsNewUser("user_new_2", "twice@example.com", "Twice", "Over").GetAsync("/api/v1/users/me", ct);
        await AsNewUser("user_new_2", "twice@example.com", "Twice", "Over").GetAsync("/api/v1/users/me", ct);

        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PremPointsDbContext>();

        var rows = await context.Users.CountAsync(u => u.WorkOSUserId == "user_new_2", ct);

        Assert.Equal(1, rows);
    }

    [Fact]
    public async Task TwoPeopleWithTheSameName_GetASuffixedUsername()
    {
        var ct = TestContext.Current.CancellationToken;

        await AsNewUser("user_new_3", "alex@one.example", "Alex", "Mack").GetAsync("/api/v1/users/me", ct);
        await AsNewUser("user_new_4", "alex@two.example", "Alex", "Mack").GetAsync("/api/v1/users/me", ct);

        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PremPointsDbContext>();

        var second = await context.Users.SingleAsync(u => u.WorkOSUserId == "user_new_4", ct);

        // Namesakes are ordinary in a league of twenty, not an error, so the
        // second is renamed rather than refused a sign-in.
        Assert.Equal("AlexMack2", second.Username);
    }

    [Fact]
    public async Task WithoutAnyProfileClaims_NoUserIsCreated()
    {
        var ct = TestContext.Current.CancellationToken;

        // What an unconfigured WorkOS JWT template looks like: the token
        // validates, but carries nothing to build a user row from. The request
        // must still be answered rather than erroring, and must not invent a
        // half-populated player.
        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(TestAuthHandler.AuthenticationScheme, "user_new_5");
        HttpClient.DefaultRequestHeaders.Remove("X-Test-Email");
        HttpClient.DefaultRequestHeaders.Remove("X-Test-First-Name");
        HttpClient.DefaultRequestHeaders.Remove("X-Test-Last-Name");

        var response = await HttpClient.GetAsync("/api/v1/users/me", ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PremPointsDbContext>();

        Assert.False(await context.Users.AnyAsync(u => u.WorkOSUserId == "user_new_5", ct));
    }

    /// <summary>
    /// A caller WorkOS knows and PremPoints does not, carrying the profile
    /// claims the JWT template supplies in production.
    /// </summary>
    private HttpClient AsNewUser(string workOsUserId, string email, string firstName, string lastName)
    {
        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(TestAuthHandler.AuthenticationScheme, workOsUserId);

        HttpClient.DefaultRequestHeaders.Remove("X-Test-Email");
        HttpClient.DefaultRequestHeaders.Remove("X-Test-First-Name");
        HttpClient.DefaultRequestHeaders.Remove("X-Test-Last-Name");

        HttpClient.DefaultRequestHeaders.Add("X-Test-Email", email);
        HttpClient.DefaultRequestHeaders.Add("X-Test-First-Name", firstName);
        HttpClient.DefaultRequestHeaders.Add("X-Test-Last-Name", lastName);

        return HttpClient;
    }
}
