using Api.Infrastructure.EntityFramework;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Time.Testing;

namespace IntegrationTests;

public class CustomWebApplicationFactory(string connectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureTestServices(services =>
        {
            // Pins "now" inside the 2025/26 season TestDataSeeder creates.
            //
            // Without this the suite depends on the wall clock: handlers default
            // their "as at" date to today, and once today fell outside the
            // seeded gameweeks every season lookup returned null and the tests
            // began failing on a date rather than on a change. Gameweek 2 runs
            // 22-28 Aug 2025, so this sits mid-season with periods either side.
            services.AddSingleton<TimeProvider>(
                new FakeTimeProvider(new DateTimeOffset(2025, 8, 25, 12, 0, 0, TimeSpan.Zero)));
            // ... your DbContext setup here ...

            // 1. Add the Test Auth Handler
            services.AddAuthentication(options =>
            {
                // Set the default scheme to our custom "Test" scheme
                options.DefaultAuthenticateScheme = TestAuthHandler.AuthenticationScheme;
                options.DefaultChallengeScheme = TestAuthHandler.AuthenticationScheme;
            })
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                TestAuthHandler.AuthenticationScheme,
                options => { }
            );
        });
        builder.ConfigureAppConfiguration(cfg =>
        {
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PremPoints"] = connectionString,

                // Cleared, not set. The API normally swaps this name into the
                // connection string's Initial Catalog so one credential can
                // serve every environment, but LocalDbHarness has already
                // created a uniquely named throwaway database and put it in the
                // string above. Leaving Development's "PremPointsDev" in place
                // would redirect every test at that shared database instead.
                ["Database:Name"] = null,
            });
        });

        builder.ConfigureServices(services =>
        {
            using var scope = services.BuildServiceProvider().CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<PremPointsDbContext>();
        });
    }
}
