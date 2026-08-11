using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace IntegrationTests;

public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string AuthenticationScheme = "Test";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // 1. Inspect the "Authorization" header. 
        // We will pass the User ID we want to act as in the header value.
        // Format: "Authorization: Test user_1"
        var authHeader = Context.Request.Headers.Authorization.ToString();

        // Default to "user_1" (Admin from your seeder) if no header is present
        var userId = "user_1";

        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith(AuthenticationScheme))
        {
            userId = authHeader.Substring(AuthenticationScheme.Length + 1).Trim();
        }

        // 2. Create the Claims
        // This MUST match what your Endpoint expects (ClaimTypes.NameIdentifier)
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, "Test User"),
            // You can add roles here too if needed:
            // new Claim(ClaimTypes.Role, "Admin") 
        };

        var identity = new ClaimsIdentity(claims, AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, AuthenticationScheme);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}