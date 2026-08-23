using Api.Infrastructure;
using Api.Infrastructure.EntityFramework;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Api.Infrastructure.Endpoints;
using ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services
    .AddApiInfrastructure(builder.Configuration) // Auth, CORS, RateLimiting, Swagger
    .AddPersistence(builder.Configuration)       // EF Core, Interceptors
    .AddApplicationServices();                   // MediatR, Validators, UserServices

// Deployed environments only. Locally the database is LocalDB, which does not
// pause and does not want a connection opened against it every half hour; the
// integration tests run as Development for the same reason.
if (!builder.Environment.IsDevelopment())
{
    builder.Services.AddHostedService<DatabaseKeepAlive>();
}

var app = builder.Build();

// 3. Request Pipeline (Middleware Order Matters!)
app.Logger.LogInformation("--- PREMPOINTS API STARTING UP ---");

// Before anything else: Railway terminates TLS at its edge and forwards plain
// HTTP, so without this the app believes every request arrived insecure.
// UseHttpsRedirection here instead would redirect a request that already came
// in over HTTPS, and keep redirecting it.
var forwardedHeaders = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
};

// The reason the headers above are honoured at all. KnownNetworks and
// KnownProxies default to loopback, so a header arriving from any other address
// is dropped — which on Railway is every header, since its edge is not on
// localhost. The app would then still read the request as HTTP, and
// UseHttpsRedirection below would bounce it back to a URL the edge forwards as
// HTTP again: a redirect loop, on every request, in production only.
//
// Clearing both means trusting whatever sits in front of us. That is sound
// here because nothing reaches the container except through Railway's proxy;
// it stops being sound the moment the app is exposed directly.
forwardedHeaders.KnownNetworks.Clear();
forwardedHeaders.KnownProxies.Clear();

app.UseForwardedHeaders(forwardedHeaders);

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
//
// /alive is exempt. Railway's healthcheck reaches the container directly on the
// internal network rather than through the edge, so the X-Forwarded-Proto the
// block above depends on is not on that request. Kestrel sees plain HTTP,
// UseHttpsRedirection answers 307, and Railway only accepts a 2xx — so the
// deploy fails its healthcheck while the app is perfectly healthy.
if (!app.Environment.IsDevelopment())
{
    app.UseWhen(
        context => !context.Request.Path.StartsWithSegments("/alive"),
        branch =>
        {
            branch.UseHsts();
            branch.UseHttpsRedirection();
        });
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

// The published image puts the built React app in wwwroot, so one container
// serves both halves: same origin, no CORS, one thing to deploy and one URL to
// hand out. Served before authentication so the login page can load its own
// assets.
app.UseDefaultFiles();

// The static file provider refuses to serve an extension it has no content
// type for, and MapFallbackToFile below would then answer index.html with a
// text/html content type. The browser rejects that as a manifest and the app
// silently stops being installable, with a 200 in the logs and nothing to
// suggest anything is wrong. So the extension is registered explicitly.
var contentTypes = new FileExtensionContentTypeProvider();
contentTypes.Mappings[".webmanifest"] = "application/manifest+json";

app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = contentTypes,
    OnPrepareResponse = context =>
    {
        var path = context.Context.Request.Path.Value ?? string.Empty;

        // Vite fingerprints everything under /assets, so the name changes
        // whenever the contents do and the old name is never reused. That is
        // exactly the case immutable describes: cache it for a year and the
        // repeat visit fetches no JavaScript or CSS at all.
        if (path.StartsWith("/assets/", StringComparison.OrdinalIgnoreCase))
        {
            context.Context.Response.Headers.CacheControl = "public, max-age=31536000, immutable";
            return;
        }

        // Everything else keeps its name across deploys — index.html, the
        // manifest, the icon, and above all sw.js, which is how every installed
        // client learns there is a new version. Cached, they would pin players
        // to the build they first visited.
        context.Context.Response.Headers.CacheControl = "no-cache";
    },
});

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter(); // Apply limits after we know who the user is

// 4. Endpoint Mapping
// Every feature endpoint declares its route without the prefix, so routes read
// "teams" and both the prefix and the version live in one place. Versioning is
// a URL segment rather than a header or a library: it is the one form every
// client, proxy and log already understands, and it costs one line here.
// The client reaches these through the Vite dev-server proxy, same-origin.
//
// RequireRateLimiting is applied here, to the group, rather than on each
// endpoint. It was per-endpoint and 10 of 18 endpoints had quietly been
// written without it: a limiter you have to remember is one you forget, and
// forgetting leaves the endpoint unlimited. An endpoint needing a different
// budget overrides this with its own RequireRateLimiting call.
app.MapFeatureEndpoints(
    app.MapGroup("api/v1")
       .RequireRateLimiting("DefaultPolicy"));

// /alive and /health, deliberately outside the "api" prefix: they are host
// concerns, and the Aspire dashboard probes /alive by absolute path.
app.MapDefaultEndpoints();

// Client-side routing: anything that is not an API route and not a real file on
// disk belongs to the React router, so hand it index.html and let the browser
// decide. Declared last so it can never shadow a real endpoint.
app.MapFallbackToFile("index.html");

// Migrate on start. A single container has no separate migration step, and the
// alternative — remembering to run dotnet ef against production by hand — is
// exactly the thing that gets forgotten. Safe while one instance runs; this
// needs revisiting before scaling out, because concurrent migrators race.
if (!app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var database = scope.ServiceProvider.GetRequiredService<PremPointsDbContext>();
    await database.Database.MigrateAsync();
}

await app.RunAsync();