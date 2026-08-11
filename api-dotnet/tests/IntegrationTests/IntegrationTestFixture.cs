using Api.Infrastructure.EntityFramework;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IntegrationTests;

/// One throwaway LocalDB database per test, migrated and seeded from scratch.
///
/// Per test, not per class, because these tests mutate shared state
/// destructively — GetActiveTeamsTests deletes every TeamSeason, and
/// DeactivateUserTests removes users. Sharing a database across a class makes
/// them order-dependent, and xUnit's order is not the source order.
///
/// The Testcontainers version this replaces had the same per-test isolation.
/// What made that expensive was starting a SQL Server container each time;
/// CREATE DATABASE plus migrations on an already-running LocalDB is far cheaper.
public sealed class IntegrationTestFixture : IAsyncLifetime
{
    private string _databaseName = null!;

    public CustomWebApplicationFactory Factory { get; private set; } = null!;
    public HttpClient HttpClient { get; private set; } = null!;
    public TestDataSeeder DataSeeder { get; private set; } = null!;
    public string ConnectionString { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        await LocalDbHarness.DropStaleDatabasesAsync();

        _databaseName = LocalDbHarness.NewDatabaseName();
        ConnectionString = LocalDbHarness.ConnectionStringFor(_databaseName);

        await LocalDbHarness.CreateDatabaseAsync(_databaseName);

        Factory = new CustomWebApplicationFactory(ConnectionString);
        HttpClient = Factory.CreateClient();

        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PremPointsDbContext>();

        await context.Database.MigrateAsync();

        DataSeeder = new TestDataSeeder(context);
        await DataSeeder.SeedAsync();
    }

    public async ValueTask DisposeAsync()
    {
        HttpClient?.Dispose();

        if (Factory is not null)
        {
            await Factory.DisposeAsync();
        }

        // Connections from the factory's pool would otherwise block the drop.
        SqlConnection.ClearAllPools();

        await LocalDbHarness.DropDatabaseAsync(_databaseName);
    }
}
