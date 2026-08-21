// Must match the redirect URI registered in the WorkOS dashboard, and
// client/src/lib/authConfig.ts assumes sign-in returns to this origin.
const int ClientPort = 57966;

var builder = DistributedApplication.CreateBuilder(args);

// Resolves ConnectionStrings:PremPoints from this project's own configuration
// (appsettings.json here, or user-secrets) and injects it into the API as the
// ConnectionStrings__PremPoints environment variable, which is what
// AddPersistence already reads. Not AddSqlServer: that would start a container,
// and there is no Docker on this machine.
var db = builder.AddConnectionString("PremPoints");

var api = builder.AddProject<Projects.Api>("prempoints-api")
                 .WithReference(db)
                 // Pinned to the port the http launch profile already uses, so
                 // http://localhost:5062/swagger is the same address whether the
                 // API is started by Aspire or on its own.
                 .WithEndpoint("http", e => e.Port = 5062)
                 // Adds a "Swagger" link next to the endpoint on the dashboard.
                 // The callback overload that returns a new annotation adds a
                 // link; the `url => { ... }` overload would replace the plain
                 // endpoint link instead.
                 .WithUrlForEndpoint("http", _ => new ResourceUrlAnnotation
                 {
                     Url = "/swagger",
                     DisplayText = "Swagger",
                 })
                 // Shallow liveness probe, so the dashboard shows the API as
                 // starting rather than running while it boots.
                 .WithHttpHealthCheck("/alive")
                 .WithExternalHttpEndpoints();

// The client port is pinned rather than left to Aspire.
//
// AddViteApp would otherwise pick a free port per run — 55160, then 62450, then
// 57966 across three starts this morning. That is fine until OAuth: WorkOS only
// redirects back to a URI registered in its dashboard, so a port that changes
// every run means sign-in fails every run. Registered once at 57966, fixed here.
//
// isProxied: false so Vite binds the port directly and the browser talks to it
// without Aspire's proxy in between, which keeps hot reload's websocket simple.
//
// API_URL is what client/vite.config.ts proxies /api to, so the client reaches
// the API same-origin and CORS never enters the picture locally.
builder.AddViteApp("prempoints-client", "../../../client")
       .WithBun()
       .WithReference(api)
       .WaitFor(api)
       .WithHttpEndpoint(port: ClientPort, targetPort: ClientPort, env: "PORT", isProxied: false)
       .WithEnvironment("API_URL", api.GetEndpoint("http"))
       .WithExternalHttpEndpoints();

builder.Build().Run();
