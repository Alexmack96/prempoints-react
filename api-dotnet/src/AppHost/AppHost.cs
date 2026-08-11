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

// No WithHttpEndpoint: AddViteApp assigns the port itself and passes it as PORT,
// which is why client/vite.config.ts reads process.env.PORT.
//
// API_URL is what client/vite.config.ts proxies /api to, so the client reaches
// the API same-origin and CORS never enters the picture locally.
builder.AddViteApp("prempoints-client", "../../../client")
       .WithBun()
       .WithReference(api)
       .WaitFor(api)
       .WithEnvironment("API_URL", api.GetEndpoint("http"))
       .WithExternalHttpEndpoints();

builder.Build().Run();
