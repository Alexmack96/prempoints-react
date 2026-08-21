using Api.Domain.Contracts;
using Api.Features.Teams;
using Api.Infrastructure.Paging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;
using System.Collections;

namespace IntegrationTests.Features;

/// <summary>
/// The shape rules — what an endpoint returns and how it names the thing it
/// operates on. Structural conventions live in EndpointConventionTests; these
/// are about the contract itself.
/// <para>
/// Where an existing endpoint breaks a rule, it is listed in ConventionDebt
/// with a reason rather than the rule being weakened. A rule with a short,
/// named exception list still stops the next endpoint; a rule left unwritten
/// stops nothing.
/// </para>
/// </summary>
public class ConventionRulesTests : BaseIntegrationTest
{
    private IReadOnlyList<RouteEndpoint> ApiEndpoints()
    {
        var source = Factory.Services.GetRequiredService<EndpointDataSource>();

        return source.Endpoints
            .OfType<RouteEndpoint>()
            .Where(e => e.RoutePattern.RawText?.StartsWith("api/", StringComparison.Ordinal) == true)
            .ToList();
    }

    private static string Route(RouteEndpoint endpoint) => endpoint.RoutePattern.RawText ?? "(no route)";

    private static bool Uses(RouteEndpoint endpoint, string method) =>
        endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods
            .Contains(method, StringComparer.OrdinalIgnoreCase) == true;

    private static IEnumerable<IProducesResponseTypeMetadata> Declared(RouteEndpoint endpoint) =>
        endpoint.Metadata.OfType<IProducesResponseTypeMetadata>();

    private static bool Declares(RouteEndpoint endpoint, int statusCode) =>
        Declared(endpoint).Any(m => m.StatusCode == statusCode);

    private static void AssertNone(IEnumerable<string> offenders, string message)
    {
        var list = offenders.ToList();
        Assert.True(list.Count == 0, $"{message}{Environment.NewLine}  {string.Join($"{Environment.NewLine}  ", list)}");
    }

    [Fact]
    public void EveryEndpointDeclaresASuccessStatus()
    {
        // Without this, a generated client gets an untyped 200 and the OpenAPI
        // document describes a request with no documented response.
        AssertNone(
            ApiEndpoints()
                .Where(e => !Declared(e).Any(m => m.StatusCode is >= 200 and < 300))
                .Select(Route),
            "Endpoints declaring no 2xx response:");
    }

    [Fact]
    public void ItemRoutesDeclareNotFound()
    {
        // Anything addressed by id can be asked for an id that does not exist.
        AssertNone(
            ApiEndpoints()
                .Where(e => Route(e).Contains("{id:guid}", StringComparison.Ordinal))
                .Where(e => !Declares(e, StatusCodes.Status404NotFound))
                .Select(Route),
            "Item routes not declaring 404:");
    }

    [Fact]
    public void DeleteEndpointsDeclareNoContentAndNotFound()
    {
        AssertNone(
            ApiEndpoints()
                .Where(e => Uses(e, HttpMethods.Delete))
                .Where(e => !Declares(e, StatusCodes.Status204NoContent) || !Declares(e, StatusCodes.Status404NotFound))
                .Select(Route),
            "DELETE endpoints must declare both 204 and 404:");
    }

    [Fact]
    public void CreateEndpointsDeclareCreated()
    {
        // A POST to a parameterless resource route creates something, and the
        // caller needs its URL. 200 leaves them to guess it.
        //
        // Endpoints answering with a collection are exempt on principle rather
        // than by exception: a bulk upsert creates many rows and there is no
        // single Location header that could describe them.
        AssertNone(
            ApiEndpoints()
                .Where(e => Uses(e, HttpMethods.Post))
                .Where(e => e.RoutePattern.Parameters.Count == 0)
                .Where(e => !Declared(e).Any(m =>
                    m.StatusCode is >= 200 and < 300 && m.Type is not null && IsBareCollection(m.Type)))
                .Where(e => !ConventionDebt.CreateReturnsOk.ContainsKey(Route(e)))
                .Where(e => !Declares(e, StatusCodes.Status201Created))
                .Select(Route),
            "Creates must answer 201 with a Location header (or be listed in ConventionDebt):");
    }

    [Fact]
    public void CollectionReadsArePaged()
    {
        // A bare array has no room for a total or a page, so a client that
        // outgrows one response has no way to ask for the rest.
        AssertNone(
            ApiEndpoints()
                .Where(e => Uses(e, HttpMethods.Get))
                .Where(e => !ConventionDebt.UnpagedCollection.ContainsKey(Route(e)))
                .Where(e => Declared(e).Any(m =>
                    m.StatusCode is >= 200 and < 300 &&
                    m.Type is not null &&
                    IsBareCollection(m.Type)))
                .Select(Route),
            "Collection reads must return PagedResponse<T> (or be listed in ConventionDebt):");
    }

    [Fact]
    public void RoutesIdentifyResourcesById()
    {
        // A name is mutable and user-supplied. A URL built from one breaks on
        // the first rename, and collides with literal segments like /teams/active.
        AssertNone(
            ApiEndpoints()
                .Where(e => !ConventionDebt.NonIdIdentity.ContainsKey(Route(e)))
                .Where(e => e.RoutePattern.Parameters.Any(p => !IsIdParameter(p)))
                .Select(Route),
            "Route parameters must be an id constrained to a guid (or be listed in ConventionDebt):");
    }

    [Fact]
    public void DtosDoNotExposeAuditColumns()
    {
        // Who last touched a row is our bookkeeping, not the client's business,
        // and publishing it makes it contract we then have to keep.
        var audit = new[] { "CreatedAtUtc", "CreatedBy", "LastModifiedUtc", "LastModifiedBy" };

        var offenders = typeof(TeamDto).Assembly
            .GetTypes()
            .Where(type => type is { IsAbstract: false, IsInterface: false })
            .Where(type => typeof(IEntityDto).IsAssignableFrom(type))
            .SelectMany(type => type.GetProperties()
                .Where(property => audit.Contains(property.Name, StringComparer.Ordinal))
                .Select(property => $"{type.Name}.{property.Name}"))
            .ToList();

        AssertNone(offenders, "DTOs must not expose audit columns:");
    }

    [Fact]
    public void WritesRequireAuthorization()
    {
        // A GET that leaks is bad; a POST anyone can call changes other people's
        // data. Nothing enforced this before, and eight write endpoints ended up
        // anonymous — including trade submission.
        var writes = new[] { HttpMethods.Post, HttpMethods.Put, HttpMethods.Patch, HttpMethods.Delete };

        AssertNone(
            ApiEndpoints()
                .Where(endpoint => writes.Any(method => Uses(endpoint, method)))
                .Where(endpoint => !ConventionDebt.AnonymousWrite.ContainsKey(Route(endpoint)))
                .Where(endpoint => !endpoint.Metadata.OfType<IAuthorizeData>().Any())
                .Select(Route),
            "Write endpoints must require authorization (or be listed in ConventionDebt):");
    }

    [Fact]
    public void ConventionDebtHasNoStaleEntries()
    {
        // A debt list that outlives the endpoint it excused turns into noise, and
        // noise is how the next real violation gets waved through. An entry that
        // no longer matches a route means the rollout already fixed it and the
        // exemption should go.
        var routes = ApiEndpoints().Select(Route).ToHashSet(StringComparer.Ordinal);

        var stale = ConventionDebt.CreateReturnsOk.Keys
            .Concat(ConventionDebt.UnpagedCollection.Keys)
            .Concat(ConventionDebt.NonIdIdentity.Keys)
            .Concat(ConventionDebt.AnonymousWrite.Keys)
            .Where(route => !routes.Contains(route))
            .Distinct(StringComparer.Ordinal);

        AssertNone(stale, "ConventionDebt lists routes that no longer exist; delete these entries:");
    }

    private static bool IsIdParameter(RoutePatternParameterPart parameter) =>
        string.Equals(parameter.Name, "id", StringComparison.Ordinal) &&
        parameter.ParameterPolicies.Any(policy =>
            string.Equals(policy.Content, "guid", StringComparison.OrdinalIgnoreCase));

    private static bool IsBareCollection(Type type)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(PagedResponse<>))
        {
            return false;
        }

        return type != typeof(string) && typeof(IEnumerable).IsAssignableFrom(type);
    }
}
