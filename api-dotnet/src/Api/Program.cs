using Api.Infrastructure;
using Api.Infrastructure.Endpoints;
using ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services
    .AddApiInfrastructure(builder.Configuration) // Auth, CORS, RateLimiting, Swagger
    .AddPersistence(builder.Configuration)       // EF Core, Interceptors
    .AddApplicationServices();                   // MediatR, Validators, UserServices

var app = builder.Build();

// 3. Request Pipeline (Middleware Order Matters!)
app.Logger.LogInformation("--- PREMPOINTS API STARTING UP ---");

app.UseExceptionHandler(); // Always first

// Gives a ProblemDetails body to responses that would otherwise be a bare
// status line — the 401 from an auth challenge, the 403 from a policy, the 404
// from an unmatched route. Without it a client parsing errors has to special-
// case "sometimes there is no body", and those three are the most common
// errors any client will hit.
app.UseStatusCodePages();

// Not in Development: under Aspire the API is served over plain HTTP, so a
// redirect to a https port that was never bound just breaks every request.
// TLS is the hosting platform's job in a deployment.
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseCors("AllowFrontend");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.MapSwaggerTokenExchange();
    app.UseSwaggerUI(options =>
    {
        // The browser half of the WorkOS authorization-code flow declared in
        // AddCustomSwaggerGen. Swagger UI is a public client — it runs entirely
        // in the browser — so it authenticates with PKCE and never a secret.
        //
        // The redirect target is /swagger/oauth2-redirect.html on whatever
        // origin this API is served from, and it must be registered as a
        // redirect URI in the WorkOS dashboard or the sign-in is rejected
        // before it starts.
        options.OAuthClientId(WorkOsOptions.FromConfiguration(app.Configuration).ClientId);
        options.OAuthUsePkce();
        options.OAuthAppName("PremPoints API (Swagger)");

        // WorkOS requires exactly one connection selector — connection,
        // organization, or provider — on the authorize call, and swagger-ui
        // sends none of them on its own. Without this the sign-in fails before
        // the login screen with sso/invalid-connection-selector. "authkit" is
        // the selector that means "use the hosted AuthKit sign-in page", which
        // is the same one the React client goes through.
        options.OAuthAdditionalQueryStringParams(new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["provider"] = "authkit",
        });
    });
}

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter(); // Apply limits after we know who the user is

// 4. Endpoint Mapping
// Every feature endpoint declares its route without the prefix, so routes read
// "teams" and both the prefix and the version live in one place. Versioning is
// a URL segment rather than a header or a library: it is the one form every
// client, proxy and log already understands, and it costs one line here.
// The client reaches these through the Vite dev-server proxy, same-origin.
app.MapFeatureEndpoints(app.MapGroup("api/v1"));

// /alive and /health, deliberately outside the "api" prefix: they are host
// concerns, and the Aspire dashboard probes /alive by absolute path.
app.MapDefaultEndpoints();

await app.RunAsync();