
using Api.Domain.Authorization;
using Api.Infrastructure.Endpoints;
using Api.Infrastructure.Endpoints.Filters;
using Api.Infrastructure.EntityFramework;
using Api.Infrastructure.Middleware;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using OpenTelemetry.Metrics;
using System.Reflection;
using System.Security.Claims;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

namespace Api.Infrastructure;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Name of the OpenAPI security scheme. Program.cs configures the Swagger UI
    /// side of the same flow, so both must agree on this string.
    /// </summary>
    public const string WorkOsSecuritySchemeId = "workos";

    public static IServiceCollection AddApiInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        // 1. Core API Logic
        services.AddEndpoints();
        services.AddEndpointsApiExplorer();
        services.AddCustomSwaggerGen();
        services.AddHttpContextAccessor();
        services.AddHttpClient(); // SwaggerTokenExchange needs IHttpClientFactory.

        // 2. JSON Configuration
        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

        // 3. Exception Handling
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        // 4. Health Checks
        services.AddHealthChecks()
            .AddSqlServer(
                // Same deferral as AddPersistence, for the same reason.
                connectionStringFactory: sp => GetPremPointsConnectionString(sp.GetRequiredService<IConfiguration>()),
                name: "sql-check",
                timeout: TimeSpan.FromSeconds(3),
                tags: ["ready"]);

        // 5. OpenTelemetry
        services.AddOpenTelemetry().WithMetrics(x => x.AddPrometheusExporter());

        // 6. Security (Rate Limiting, CORS, Auth)
        services.AddCustomRateLimiting();
        services.AddCustomCors(config);
        services.AddWorkOsAuthentication(config);

        return services;
    }

    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<AuditableEntityInterceptor>();
        services.AddDbContext<PremPointsDbContext>((sp, options) =>
        {
            var interceptor = sp.GetRequiredService<AuditableEntityInterceptor>();
            // Resolved here rather than at registration: WebApplicationFactory
            // layers the integration tests' throwaway LocalDB connection string
            // on after AddPersistence has run, so an eager read would capture
            // the value that exists before the tests get their say.
            options
                .UseSqlServer(
                    GetPremPointsConnectionString(sp.GetRequiredService<IConfiguration>()),
                    sql => sql.EnableRetryOnFailure())
                .AddInterceptors(interceptor);
        });

        return services;
    }

    /// <summary>
    /// Reads the one connection string this API has, points it at the database
    /// this environment owns, and refuses to start without it.
    /// <para>
    /// Every environment shares one server and one SQL login, and differs only
    /// in which database it targets — PremPointsDev locally, PremPoints in
    /// production. So the secret is environment-agnostic and the database name
    /// is not a secret at all: it is committed per environment as Database:Name
    /// and swapped into the Initial Catalog here. Keeping one credential rather
    /// than one string per environment is what stops a stale secret in one
    /// environment from pointing at another environment's data.
    /// </para>
    /// <para>
    /// There is deliberately no appsettings fallback for the string itself: a
    /// fallback would let a missing secret quietly redirect reads and writes.
    /// </para>
    /// </summary>
    private static string GetPremPointsConnectionString(IConfiguration config)
    {
        var connectionString = config.GetConnectionString("PremPoints");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:PremPoints is not configured. It holds a password, so it " +
                "lives in user-secrets rather than appsettings.json. Set it with:\n" +
                "  dotnet user-secrets set \"ConnectionStrings:PremPoints\" \"<string>\" --project src/AppHost\n" +
                "  dotnet user-secrets set \"ConnectionStrings:PremPoints\" \"<string>\" --project src/Api");
        }

        // Absent only for the integration tests, which own their whole
        // connection string and clear this key so the throwaway LocalDB database
        // they just created is not renamed out from under them.
        var databaseName = config["Database:Name"];
        if (string.IsNullOrWhiteSpace(databaseName))
        {
            return connectionString;
        }

        return new SqlConnectionStringBuilder(connectionString)
        {
            InitialCatalog = databaseName,
        }.ConnectionString;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // The clock is an input. Handlers take TimeProvider and call
        // GetUtcNow() rather than reading DateTime.UtcNow, so a test can pin
        // "now" to any instant — which is what the integration tests need, since
        // a season lookup that depends on the real date rots the moment the
        // seeded season is in the past.
        services.AddSingleton(TimeProvider.System);

        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
        });

        return services;
    }

    // --- Private Helpers to keep the main extensions clean ---

    private static IServiceCollection AddCustomSwaggerGen(this IServiceCollection services)
    {
        return services.AddSwaggerGen(options =>
        {
            // Every vertical slice nests its own Request/Command type, so the
            // default schemaId — the short type name — collides the moment two
            // slices both have a "Request", and document generation fails
            // outright. Qualify by namespace: SeedNewSeason.Request and
            // DeactivateUser.Request are then distinct.
            options.CustomSchemaIds(type => type.FullName!
                .Replace("Api.Features.", "", StringComparison.Ordinal)
                .Replace('+', '.'));

            // Authorization code + PKCE against WorkOS AuthKit, so "Authorize"
            // in Swagger UI runs a real sign-in and the token it gets back is
            // the same kind of token the React client sends. No client secret
            // is configured on purpose: it would sit in a browser, and WorkOS
            // accepts the code_verifier in its place.
            options.AddSecurityDefinition(WorkOsSecuritySchemeId, new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.OAuth2,
                Description = "Sign in with WorkOS AuthKit. Uses PKCE; no client secret required.",
                Flows = new OpenApiOAuthFlows
                {
                    AuthorizationCode = new OpenApiOAuthFlow
                    {
                        AuthorizationUrl = WorkOsOptions.AuthorizationUrl,
                        // Our own endpoint, not WorkOS's. The browser cannot read
                        // a response from api.workos.com — no CORS headers — so
                        // the exchange happens server-side. See SwaggerTokenExchange.
                        TokenUrl = new Uri(SwaggerTokenExchange.TokenPath, UriKind.Relative),
                        // WorkOS's authorize endpoint takes no `scope`
                        // parameter, so leaving this empty is correct — adding
                        // scopes here would make swagger-ui send one.
                        Scopes = new Dictionary<string, string>(StringComparer.Ordinal),
                    },
                },
            });

            // Applies that scheme per-operation rather than document-wide, so
            // the padlocks match what RequireAuthorization actually enforces.
            options.OperationFilter<SecurityRequirementOperationFilter>();
        });
    }

    /// <summary>
    /// The default budget: 120 requests a minute, per caller.
    /// <para>
    /// Per caller is the whole point. <c>AddFixedWindowLimiter</c> builds one
    /// limiter shared by every request using the policy, so the budget was
    /// global — a dozen players on a Saturday afternoon would have spent each
    /// other's allowance and 429'd one another, and one impatient client could
    /// have locked out the league. Partitioning gives each caller their own
    /// window, which is what the number was always meant to describe.
    /// </para>
    /// <para>
    /// Identity first, address second: signed-in players are told apart even
    /// when several sit behind one office NAT, and an anonymous caller still
    /// gets a bounded share. The limiter is per instance, so this is a courtesy
    /// limit rather than a defence against a distributed flood — that belongs
    /// at the edge.
    /// </para>
    /// </summary>
    private static IServiceCollection AddCustomRateLimiting(this IServiceCollection services)
    {
        return services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy("DefaultPolicy", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    PartitionKeyFor(httpContext),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 120,
                        Window = TimeSpan.FromSeconds(60),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0,
                    }));
        });
    }

    /// <summary>
    /// Who to charge a request to. The internal user id when we have one, the
    /// WorkOS subject when the token validated but the row is missing, and the
    /// remote address otherwise. The final fallback is a shared bucket: an
    /// unknown caller with no address should still not be unlimited.
    /// </summary>
    private static string PartitionKeyFor(HttpContext httpContext)
    {
        var user = httpContext.User;

        var internalUserId = user.FindFirst("InternalUserId")?.Value;
        if (!string.IsNullOrEmpty(internalUserId))
        {
            return $"user:{internalUserId}";
        }

        var externalId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(externalId))
        {
            return $"workos:{externalId}";
        }

        var address = httpContext.Connection.RemoteIpAddress;

        return address is null ? "anonymous" : $"ip:{address}";
    }

    private static IServiceCollection AddCustomCors(this IServiceCollection services, IConfiguration config)
    {
        var allowedOrigins = config.GetSection("AllowedOrigins").Get<string[]>() ?? [];
        return services.AddCors(options =>
        {
            options.AddPolicy("AllowFrontend", policy =>
            {
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });
        });
    }

    private static IServiceCollection AddWorkOsAuthentication(this IServiceCollection services, IConfiguration config)
    {
        var workOsIssuer = WorkOsOptions.FromConfiguration(config).Issuer;

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.Authority = workOsIssuer;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = workOsIssuer,
                ValidateAudience = false,
                ValidateLifetime = true,
            };

            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = _ => Task.CompletedTask,
                OnTokenValidated = async context =>
                {
                    // Logic extracted to keep Program.cs clean
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                    var externalId = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                    logger.LogInformation("Token Validated for User: {ExternalId}", externalId);

                    if (string.IsNullOrEmpty(externalId)) return;

                    var dbContext = context.HttpContext.RequestServices.GetRequiredService<PremPointsDbContext>();

                    // Note: In high traffic production, consider caching this user lookup 
                    // to avoid hitting the DB on every single API request.
                    var user = await dbContext.Users
                        .AsNoTracking()
                        .FirstOrDefaultAsync(u => u.WorkOSUserId == externalId);

                    if (user is null) return;

                    if (context.Principal?.Identity is ClaimsIdentity claimsIdentity)
                    {
                        claimsIdentity.AddClaim(new Claim("InternalUserId", user.Id.ToString()));

                        

                        // The role is ours, not WorkOS's — it lives on the user row, so it

                        // has to be projected onto the principal here or Policies.Admin can

                        // never be satisfied. Added as a role claim so the built-in

                        // RequireRole does the work.

                        claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, user.Role.ToString()));
                    }
                }
            };
        });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(Policies.Admin, policy =>
                policy.RequireRole(nameof(UserRole.Administrator)));
        });

        return services;
    }
}