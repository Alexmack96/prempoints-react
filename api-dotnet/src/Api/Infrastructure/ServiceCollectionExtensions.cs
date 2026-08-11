
using Api.Infrastructure.Endpoints;
using Api.Infrastructure.EntityFramework;
using Api.Infrastructure.Middleware;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Metrics;
using System.Reflection;
using System.Security.Claims;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

namespace Api.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        // 1. Core API Logic
        services.AddEndpoints();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            // Every vertical slice nests its own Request/Command type, so the
            // default schemaId — the short type name — collides the moment two
            // slices both have a "Request", and document generation fails
            // outright. Qualify by namespace: SeedNewSeason.Request and
            // DeactivateUser.Request are then distinct.
            options.CustomSchemaIds(type => type.FullName!
                .Replace("Api.Features.", "", StringComparison.Ordinal)
                .Replace('+', '.'));
        });
        services.AddHttpContextAccessor();

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
                connectionString: config.GetConnectionString("PremPoints")!,
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
            options
                .UseSqlServer(config.GetConnectionString("PremPoints"))
                .AddInterceptors(interceptor);
        });

        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
        });

        return services;
    }

    // --- Private Helpers to keep the main extensions clean ---

    private static IServiceCollection AddCustomRateLimiting(this IServiceCollection services)
    {
        return services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddFixedWindowLimiter("DefaultPolicy", opt =>
            {
                opt.PermitLimit = 120;
                opt.Window = TimeSpan.FromSeconds(60);
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 0;
            });
        });
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
        var workOsClientId = config["WorkOS:ClientId"];
        var workOsIssuer = $"https://api.workos.com/user_management/{workOsClientId}";

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
                    }
                }
            };
        });

        services.AddAuthorization();
        return services;
    }
}