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
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter(); // Apply limits after we know who the user is

// 4. Endpoint Mapping
// Every feature endpoint declares its route without the prefix, so routes read
// "teams" and the prefix lives in one place. The client reaches these through
// the Vite dev-server proxy on /api, same-origin.
app.MapFeatureEndpoints(app.MapGroup("api"));

// /alive and /health, deliberately outside the "api" prefix: they are host
// concerns, and the Aspire dashboard probes /alive by absolute path.
app.MapDefaultEndpoints();

await app.RunAsync();