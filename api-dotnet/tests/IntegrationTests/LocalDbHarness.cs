using Microsoft.Data.SqlClient;

namespace IntegrationTests;

/// Creates and drops a throwaway database on the machine's LocalDB instance.
///
/// This replaces Testcontainers.MsSql. That needs Docker, and there is no Docker
/// on this machine — the same reason the Aspire AppHost runs the API as a
/// project rather than from its Dockerfile.
///
/// The trade is real and worth stating: LocalDB is not the SQL Server a
/// deployment runs on. It is close enough for the schema these migrations
/// produce and the queries these tests make, and it is what is actually
/// installed here.
internal static class LocalDbHarness
{
    private const string MasterConnectionString =
        "Server=(localdb)\\MSSQLLocalDB;Database=master;Trusted_Connection=True;TrustServerCertificate=True;";

    private const string DatabasePrefix = "PremPointsTests_";

    internal static string NewDatabaseName() =>
        $"{DatabasePrefix}{Guid.NewGuid():N}";

    internal static string ConnectionStringFor(string databaseName) =>
        $"Server=(localdb)\\MSSQLLocalDB;Database={databaseName};Trusted_Connection=True;TrustServerCertificate=True;";

    internal static async Task CreateDatabaseAsync(string databaseName)
    {
        await using var connection = new SqlConnection(MasterConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = $"CREATE DATABASE [{databaseName}];";
        await command.ExecuteNonQueryAsync();
    }

    internal static async Task DropDatabaseAsync(string databaseName)
    {
        await using var connection = new SqlConnection(MasterConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        // SINGLE_USER WITH ROLLBACK IMMEDIATE: the API's connection pool may
        // still hold connections when the fixture tears down, and DROP DATABASE
        // fails while anything is connected.
        command.CommandText = $"""
            IF DB_ID('{databaseName}') IS NOT NULL
            BEGIN
                ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                DROP DATABASE [{databaseName}];
            END
            """;
        await command.ExecuteNonQueryAsync();
    }

    /// Sweeps databases left behind by a run that crashed or was stopped in the
    /// debugger. Without this, LocalDB slowly fills with orphans.
    ///
    /// The age filter matters: xUnit runs test classes in parallel, so a naive
    /// "drop everything with this prefix" would delete the database belonging to
    /// a class running right now. An hour is far longer than the suite takes.
    internal static async Task DropStaleDatabasesAsync()
    {
        await using var connection = new SqlConnection(MasterConnectionString);
        await connection.OpenAsync();

        var stale = new List<string>();

        await using (var query = connection.CreateCommand())
        {
            query.CommandText =
                $"""
                SELECT name FROM sys.databases
                WHERE name LIKE '{DatabasePrefix}%'
                  AND create_date < DATEADD(hour, -1, GETDATE());
                """;

            await using var reader = await query.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                stale.Add(reader.GetString(0));
            }
        }

        foreach (var databaseName in stale)
        {
            try
            {
                await DropDatabaseAsync(databaseName);
            }
            catch (SqlException)
            {
                // Another test run may own it. Leave it alone.
            }
        }
    }
}
