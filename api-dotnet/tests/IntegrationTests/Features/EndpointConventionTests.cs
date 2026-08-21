using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace IntegrationTests.Features;

/// <summary>
/// The REST conventions in the README, as tests.
/// <para>
/// Written because the conventions had already rotted while written down only
/// as prose: ten of eighteen endpoints had been added without a rate limiter,
/// and two endpoints required authorization while documenting neither 401 nor
/// 403. Every one of those was a copy-paste omission that no reviewer caught,
/// and none of them could fail a test until now.
/// </para>
/// <para>
/// These read the endpoints the application actually mapped rather than
/// reflecting over the <c>IEndpoint</c> types, so a convention applied at the
/// route group counts, and an endpoint that is written but never mapped does
/// not.
/// </para>
/// </summary>
public class EndpointConventionTests : BaseIntegrationTest
{
    /// The API surface. /alive and /health are deliberately outside the version
    /// prefix — they are host concerns the Aspire dashboard probes by absolute
    /// path — so they are not held to these rules.
    private IReadOnlyList<RouteEndpoint> ApiEndpoints()
    {
        var source = Factory.Services.GetRequiredService<EndpointDataSource>();

        return source.Endpoints
            .OfType<RouteEndpoint>()
            .Where(endpoint => endpoint.RoutePattern.RawText?.StartsWith("api/", StringComparison.Ordinal) == true)
            .ToList();
    }

    private static string Route(RouteEndpoint endpoint) => endpoint.RoutePattern.RawText ?? "(no route)";

    [Fact]
    public void EveryEndpointIsMappedUnderTheVersionPrefix()
    {
        var source = Factory.Services.GetRequiredService<EndpointDataSource>();

        var unversioned = source.Endpoints
            .OfType<RouteEndpoint>()
            .Select(Route)
            .Where(route => route.StartsWith("api/", StringComparison.Ordinal))
            .Where(route => !route.StartsWith("api/v1/", StringComparison.Ordinal))
            .ToList();

        Assert.True(unversioned.Count == 0,
            $"Endpoints outside the version prefix: {string.Join(", ", unversioned)}");
    }

    [Fact]
    public void EveryEndpointIsRateLimited()
    {
        var unlimited = ApiEndpoints()
            .Where(endpoint => endpoint.Metadata.GetMetadata<EnableRateLimitingAttribute>() is null)
            .Select(Route)
            .ToList();

        Assert.True(unlimited.Count == 0,
            "Every endpoint must be rate limited. The group applies DefaultPolicy, so a gap here " +
            $"means something opted out: {string.Join(", ", unlimited)}");
    }

    [Fact]
    public void EveryEndpointHasAName()
    {
        // WithName is what LinkGenerator resolves a Location header from, and
        // what OpenAPI uses as the operationId. An unnamed endpoint cannot be
        // linked to, and generated clients get a name derived from its route.
        var unnamed = ApiEndpoints()
            .Where(endpoint => endpoint.Metadata.GetMetadata<IEndpointNameMetadata>() is null)
            .Select(Route)
            .ToList();

        Assert.True(unnamed.Count == 0,
            $"Endpoints missing WithName: {string.Join(", ", unnamed)}");
    }

    [Fact]
    public void EndpointNamesAreUnique()
    {
        var duplicates = ApiEndpoints()
            .Select(endpoint => endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName)
            .Where(name => name is not null)
            .GroupBy(name => name, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key!)
            .ToList();

        Assert.True(duplicates.Count == 0,
            $"Duplicate endpoint names: {string.Join(", ", duplicates)}");
    }

    [Fact]
    public void EveryEndpointIsTagged()
    {
        var untagged = ApiEndpoints()
            .Where(endpoint => endpoint.Metadata.GetMetadata<ITagsMetadata>() is null)
            .Select(Route)
            .ToList();

        Assert.True(untagged.Count == 0,
            $"Endpoints missing WithTags, which groups them in the OpenAPI document: {string.Join(", ", untagged)}");
    }

    [Theory]
    [InlineData(StatusCodes.Status401Unauthorized)]
    [InlineData(StatusCodes.Status403Forbidden)]
    public void SecuredEndpointsDeclareTheirAuthFailures(int statusCode)
    {
        // A client cannot handle a status the contract never mentions. These two
        // are the most commonly forgotten because the framework produces them
        // rather than the handler.
        var offenders = ApiEndpoints()
            .Where(RequiresAuthorization)
            .Where(endpoint => !Declares(endpoint, statusCode))
            .Select(Route)
            .ToList();

        Assert.True(offenders.Count == 0,
            $"Endpoints requiring authorization but not declaring {statusCode}: {string.Join(", ", offenders)}");
    }

    private static bool RequiresAuthorization(RouteEndpoint endpoint) =>
        endpoint.Metadata.GetMetadata<IAuthorizeData>() is not null &&
        endpoint.Metadata.GetMetadata<IAllowAnonymous>() is null;

    private static bool Declares(RouteEndpoint endpoint, int statusCode) =>
        endpoint.Metadata
            .OfType<IProducesResponseTypeMetadata>()
            .Any(metadata => metadata.StatusCode == statusCode);
}
