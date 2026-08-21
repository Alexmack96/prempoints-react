using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Api.Infrastructure.Endpoints.Filters;

/// <summary>
/// Marks only the operations that actually require authorization, by reading the
/// same endpoint metadata the authorization middleware reads.
/// <para>
/// The alternative — a document-level security requirement — would hang a
/// padlock on every operation including the anonymous ones, so the document
/// would claim more than the API enforces. Here the padlock in Swagger UI means
/// what it says, and swagger-ui only sends the bearer header where it belongs.
/// </para>
/// </summary>
internal sealed class SecurityRequirementOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(context);

        var metadata = context.ApiDescription.ActionDescriptor.EndpointMetadata;

        // AllowAnonymous wins over any authorization data, exactly as it does in
        // the authorization middleware.
        if (metadata.OfType<IAllowAnonymous>().Any()) return;
        if (!metadata.OfType<IAuthorizeData>().Any()) return;

        operation.Security =
        [
            new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(ServiceCollectionExtensions.WorkOsSecuritySchemeId, context.Document)] = [],
            }
        ];
    }
}
