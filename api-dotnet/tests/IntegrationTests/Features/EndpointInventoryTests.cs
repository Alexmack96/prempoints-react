using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.CompilerServices;
using System.Text;

namespace IntegrationTests.Features;

/// <summary>
/// The whole public surface, as one snapshot.
/// <para>
/// The convention tests each assert one named rule. This asserts nothing in
/// particular and therefore catches everything: adding, removing or re-shaping
/// an endpoint shows up as a diff that a reviewer has to look at and accept.
/// It is the cheapest guard against the failure mode these tests exist for —
/// a new slice that quietly does something the conventions did not anticipate.
/// </para>
/// </summary>
public class EndpointInventoryTests : BaseIntegrationTest
{
    [Fact]
    public Task ApiSurface()
    {
        var source = Factory.Services.GetRequiredService<EndpointDataSource>();

        var lines = source.Endpoints
            .OfType<RouteEndpoint>()
            .Where(e => e.RoutePattern.RawText?.StartsWith("api/", StringComparison.Ordinal) == true)
            .Select(Describe)
            .OrderBy(line => line, StringComparer.Ordinal)
            .ToList();

        return VerifyText(string.Join(Environment.NewLine, lines));
    }

    private static string Describe(RouteEndpoint endpoint)
    {
        var methods = endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods ?? [];
        var method = string.Join("/", methods.OrderBy(m => m, StringComparer.Ordinal));

        var statuses = endpoint.Metadata
            .OfType<IProducesResponseTypeMetadata>()
            .Select(m => m.Type is null or { Name: "Void" }
                ? m.StatusCode.ToString(System.Globalization.CultureInfo.InvariantCulture)
                : $"{m.StatusCode}:{FriendlyName(m.Type)}")
            .Distinct(StringComparer.Ordinal)
            .OrderBy(s => s, StringComparer.Ordinal);

        var builder = new StringBuilder()
            .Append(method.PadRight(6))
            .Append(' ')
            .Append((endpoint.RoutePattern.RawText ?? "?").PadRight(32))
            .Append(' ')
            .Append(AuthDescription(endpoint).PadRight(10))
            .Append(' ')
            .Append(string.Join(" ", statuses));

        return builder.ToString().TrimEnd();
    }

    private static string AuthDescription(RouteEndpoint endpoint)
    {
        if (endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null)
        {
            return "anon";
        }

        var authorize = endpoint.Metadata.OfType<IAuthorizeData>().ToList();
        if (authorize.Count == 0)
        {
            return "anon";
        }

        var policies = authorize
            .Select(a => a.Policy)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToList();

        return policies.Count == 0 ? "auth" : string.Join("+", policies);
    }

    /// Renders PagedResponse`1[TeamDto] as PagedResponse<TeamDto>, so the
    /// snapshot reads like the contract rather than like reflection output.
    private static string FriendlyName(Type type)
    {
        if (!type.IsGenericType)
        {
            return type.Name;
        }

        var name = type.Name[..type.Name.IndexOf('`', StringComparison.Ordinal)];
        var args = string.Join(", ", type.GetGenericArguments().Select(FriendlyName));

        return $"{name}<{args}>";
    }

    private static SettingsTask VerifyText(string text, [CallerFilePath] string sourceFile = "") =>
        Verify(text, sourceFile: sourceFile);
}
