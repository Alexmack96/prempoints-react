using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IntegrationTests;

/// xUnit builds a new test-class instance per test method, so owning the
/// fixture here gives every test its own database. See IntegrationTestFixture
/// for why that isolation is not optional for this suite.
public abstract class BaseIntegrationTest : IAsyncLifetime
{
    private readonly IntegrationTestFixture _fixture = new();

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    protected HttpClient HttpClient => _fixture.HttpClient;
    protected CustomWebApplicationFactory Factory => _fixture.Factory;
    protected TestDataSeeder DataSeeder => _fixture.DataSeeder;

    public ValueTask InitializeAsync() => _fixture.InitializeAsync();

    public ValueTask DisposeAsync() => _fixture.DisposeAsync();

    protected async Task<T?> DeserializeAsync<T>(HttpResponseMessage response, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(response);
        return await response.Content.ReadFromJsonAsync<T>(_jsonOptions, ct);
    }
}
