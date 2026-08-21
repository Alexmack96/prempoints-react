namespace Api.Infrastructure;

/// <summary>
/// WorkOS AuthKit coordinates for one environment. Everything here is derived
/// from the client id, and matches the discovery document served at
/// <c>{Issuer}/.well-known/openid-configuration</c> — that document is the
/// source of truth for these three values, so if WorkOS ever moves an endpoint,
/// check it before editing.
/// </summary>
public sealed class WorkOsOptions
{
    public const string SectionName = "WorkOS";

    private WorkOsOptions(string clientId) => ClientId = clientId;

    public string ClientId { get; }

    /// <summary>
    /// Token issuer, and the JWT bearer Authority. WorkOS mints access tokens
    /// with this as <c>iss</c> and publishes the signing keys under it.
    /// </summary>
    public string Issuer => $"https://api.workos.com/user_management/{ClientId}";

    /// <summary>Where Swagger UI sends the browser to sign in.</summary>
    public static Uri AuthorizationUrl { get; } = new("https://api.workos.com/user_management/authorize");

    /// <summary>
    /// Where Swagger UI exchanges the authorization code for an access token.
    /// WorkOS names this endpoint "authenticate" rather than "token", but it
    /// accepts the standard form-encoded <c>grant_type=authorization_code</c>
    /// body that swagger-ui posts, and it allows the exchange from a browser.
    /// </summary>
    public static Uri TokenUrl { get; } = new("https://api.workos.com/user_management/authenticate");

    public static WorkOsOptions FromConfiguration(IConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(config);

        var clientId = config[$"{SectionName}:ClientId"];
        if (string.IsNullOrWhiteSpace(clientId))
        {
            throw new InvalidOperationException(
                $"{SectionName}:ClientId is not configured. Without it the JWT bearer authority " +
                "resolves to a WorkOS environment that does not exist and every request 401s.");
        }

        return new WorkOsOptions(clientId);
    }
}
