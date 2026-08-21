using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Api.Infrastructure;

/// <summary>
/// Server-side half of Swagger UI's WorkOS sign-in: it takes the authorization
/// code from the browser and trades it for an access token itself.
/// <para>
/// Swagger UI would otherwise POST straight to
/// api.workos.com/user_management/authenticate from the page, which is a
/// cross-origin request that WorkOS does not answer with the CORS headers a
/// browser needs to read the response. The login succeeds and the exchange then
/// fails, which reads as a broken login. Routing it through the API makes the
/// call same-origin, so the browser is satisfied and WorkOS sees an ordinary
/// server-to-server request.
/// </para>
/// <para>
/// Development only, and deliberately so — this exists to make the Swagger page
/// usable, not as an authentication endpoint for the React client, which runs
/// its own AuthKit flow.
/// </para>
/// </summary>
public static class SwaggerTokenExchange
{
    /// <summary>
    /// Path Swagger UI posts to. Relative, so it resolves against whichever
    /// origin the page is being served from and stays same-origin on any port.
    /// </summary>
    public const string TokenPath = "/swagger-oauth/token";

    public static IEndpointRouteBuilder MapSwaggerTokenExchange(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost(TokenPath, async (
            HttpContext context,
            IHttpClientFactory httpClientFactory,
            IConfiguration config,
            CancellationToken ct) =>
        {
            if (!context.Request.HasFormContentType)
            {
                return Results.BadRequest("Expected an application/x-www-form-urlencoded token request.");
            }

            var form = await context.Request.ReadFormAsync(ct);

            // client_id comes from configuration rather than the form: the
            // browser is free to send anything, and there is exactly one client
            // this API will exchange codes for.
            var payload = new JsonObject
            {
                ["grant_type"] = "authorization_code",
                ["client_id"] = WorkOsOptions.FromConfiguration(config).ClientId,
                ["code"] = form["code"].ToString(),
                ["code_verifier"] = form["code_verifier"].ToString(),
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, WorkOsOptions.TokenUrl)
            {
                Content = new StringContent(payload.ToJsonString(), Encoding.UTF8, MediaTypeNames.Application.Json),
            };

            var httpClient = httpClientFactory.CreateClient();
            using var response = await httpClient.SendAsync(request, ct);
            var body = await response.Content.ReadAsStringAsync(ct);

            return Results.Content(
                response.IsSuccessStatusCode ? AddBearerTokenType(body) : body,
                MediaTypeNames.Application.Json,
                Encoding.UTF8,
                (int)response.StatusCode);
        })
        .AllowAnonymous()
        .ExcludeFromDescription(); // Swagger's own plumbing has no place in the API document.

        return app;
    }

    /// <summary>
    /// WorkOS returns access_token without the token_type that OAuth2 clients
    /// expect. swagger-ui falls back to "Bearer" when it is missing, so this is
    /// belt and braces rather than a fix — but it costs nothing and removes a
    /// silent dependency on that fallback.
    /// </summary>
    private static string AddBearerTokenType(string body)
    {
        try
        {
            if (JsonNode.Parse(body) is not JsonObject json)
            {
                return body;
            }

            if (!json.ContainsKey("token_type"))
            {
                json["token_type"] = "Bearer";
            }

            return json.ToJsonString();
        }
        catch (JsonException)
        {
            // Not JSON we understand. Hand it back untouched so the real
            // response still reaches the browser.
            return body;
        }
    }
}
