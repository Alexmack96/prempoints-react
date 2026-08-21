using Api.Infrastructure.EntityFramework;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;

namespace IntegrationTests;

/// xUnit builds a new test-class instance per test method, so owning the
/// fixture here gives every test its own database. See IntegrationTestFixture
/// for why that isolation is not optional for this suite.
public abstract class BaseIntegrationTest : IAsyncLifetime
{
    private readonly IntegrationTestFixture _fixture = new();

    protected HttpClient HttpClient => _fixture.HttpClient;
    protected CustomWebApplicationFactory Factory => _fixture.Factory;
    protected TestDataSeeder DataSeeder => _fixture.DataSeeder;

    public ValueTask InitializeAsync() => _fixture.InitializeAsync();

    public ValueTask DisposeAsync() => _fixture.DisposeAsync();

    /// <summary>
    /// The seeded WorkOS identities, named by what they prove rather than by
    /// which row they are. A test asking for <see cref="AsStandardUser"/> is
    /// saying "someone without admin rights", and should keep meaning that if
    /// the seed data is reshuffled.
    /// </summary>
    protected HttpClient AsAdmin() => AsUser("user_1");

    protected HttpClient AsStandardUser() => AsUser("user_2");

    protected HttpClient AsUser(string workOsUserId)
    {
        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(TestAuthHandler.AuthenticationScheme, workOsUserId);
        return HttpClient;
    }

    protected HttpClient AsAnonymous()
    {
        HttpClient.DefaultRequestHeaders.Authorization = null;
        return HttpClient;
    }

    /// <summary>
    /// Verifies a whole HTTP response — status line, headers and body — as one
    /// snapshot, so a change to any part of the contract shows up as a diff.
    /// <para>
    /// sourceFile is forwarded from the calling test rather than defaulted
    /// here: Verify derives the snapshot directory from it, and without the
    /// forward every snapshot in the suite would be filed against this file.
    /// </para>
    /// <para>
    /// traceId is scrubbed because it is per-request by definition. Nothing
    /// else is: the clock is pinned and seeded ids are deterministic, so the
    /// rest of the response should be byte-identical run to run.
    /// </para>
    /// </summary>
    protected static SettingsTask VerifyResponse(
        HttpResponseMessage response,
        [CallerFilePath] string sourceFile = "")
        => Verify(response, sourceFile: sourceFile)
            .ScrubMember("traceId")
            // An empty collection is part of this API's contract — a filter that
            // matches nothing returns items: [], not a missing property — and
            // Verify omits empty collections from the snapshot by default.
            .DontIgnoreEmptyCollections()
            // Guids inside strings — the one in the Location header of a 201 —
            // are not caught by the default scrubbing, which only sees Guid
            // values. Without this the Location header churns on every run,
            // since Guid v7 is only partly derived from the pinned clock.
            .ScrubInlineGuids();

    /// <summary>
    /// Runs work against the same database the API is using, for arranging
    /// state no endpoint can reach and for asserting on what actually landed.
    /// </summary>
    protected async Task<T> WithDbAsync<T>(Func<PremPointsDbContext, Task<T>> work)
    {
        ArgumentNullException.ThrowIfNull(work);

        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PremPointsDbContext>();
        return await work(context);
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    protected static async Task<T?> DeserializeAsync<T>(HttpResponseMessage response, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(response);
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions, ct);
    }

    protected Task WithDbAsync(Func<PremPointsDbContext, Task> work)
    {
        ArgumentNullException.ThrowIfNull(work);

        return WithDbAsync<object?>(async context =>
        {
            await work(context);
            return null;
        });
    }
}
