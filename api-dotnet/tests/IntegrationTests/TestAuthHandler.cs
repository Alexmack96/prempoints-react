using Api.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace IntegrationTests;

/// <summary>
/// Stands in for the WorkOS JWT bearer handler. A test says who it is with
/// <c>Authorization: Test user_2</c>.
/// <para>
/// It builds the principal the same way production does — look the WorkOS id up
/// in our own database, then project that row's internal id and role onto the
/// principal as claims. Hard-coding a role from the header instead would make
/// every authorization test agree with itself and prove nothing: the thing worth
/// testing is that a Standard user really is refused by
/// <c>Policies.Admin</c>.
/// </para>
/// </summary>
public class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IServiceScopeFactory scopeFactory)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string AuthenticationScheme = "Test";

    /// The seeded administrator, so a test that does not care about identity
    /// still gets through the Admin policy without saying so every time.
    private const string DefaultUserId = "user_1";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authHeader = Context.Request.Headers.Authorization.ToString();

        // No header at all means "not signed in", so the endpoint can return the
        // 401 it promises. Previously this defaulted to user_1, which made an
        // anonymous request indistinguishable from an admin one and left the
        // 401 path untestable.
        if (string.IsNullOrWhiteSpace(authHeader))
        {
            return AuthenticateResult.NoResult();
        }

        if (!authHeader.StartsWith(AuthenticationScheme, StringComparison.Ordinal))
        {
            return AuthenticateResult.Fail("Unsupported scheme.");
        }

        var workOsUserId = authHeader[AuthenticationScheme.Length..].Trim();
        if (string.IsNullOrWhiteSpace(workOsUserId))
        {
            workOsUserId = DefaultUserId;
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, workOsUserId),
            new(ClaimTypes.Name, workOsUserId),
        };

        // The profile claims a WorkOS JWT template would carry, so a test can
        // exercise first sign-in provisioning. Sent as headers because that is
        // the only channel this handler has; production reads the same claim
        // names off the validated token.
        AddIfPresent(claims, WorkOsProfileClaims.Email, "X-Test-Email");
        AddIfPresent(claims, WorkOsProfileClaims.FirstName, "X-Test-First-Name");
        AddIfPresent(claims, WorkOsProfileClaims.LastName, "X-Test-Last-Name");

        var identity = new ClaimsIdentity(claims, AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        using var scope = scopeFactory.CreateScope();
        var provisioner = scope.ServiceProvider.GetRequiredService<IUserProvisioner>();

        // The same service production calls, so provisioning is covered by the
        // suite rather than only existing on the JwtBearer path the tests
        // replace wholesale.
        var user = await provisioner.ResolveAsync(principal, Context.RequestAborted);

        // An unknown WorkOS id with no profile claims authenticates but carries
        // no role, which is exactly what production does — the token is valid,
        // the person just is not a player yet.
        if (user is not null)
        {
            identity.AddClaim(new Claim("InternalUserId", user.Id.ToString()));
            identity.AddClaim(new Claim(ClaimTypes.Role, user.Role.ToString()));
        }

        var ticket = new AuthenticationTicket(principal, AuthenticationScheme);

        return AuthenticateResult.Success(ticket);
    }
    /// Copies a test header onto the principal as the claim a WorkOS JWT
    /// template would have supplied. Absent header, absent claim — which is
    /// what an unconfigured template looks like.
    private void AddIfPresent(List<Claim> claims, string claimType, string headerName)
    {
        var value = Context.Request.Headers[headerName].ToString();

        if (!string.IsNullOrWhiteSpace(value))
        {
            claims.Add(new Claim(claimType, value));
        }
    }
}
