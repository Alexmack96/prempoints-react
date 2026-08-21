using Api.Infrastructure.Endpoints.Filters;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;

namespace Api.Infrastructure.Endpoints;

public static class EndpointExtensions
{
    /// <summary>
    /// Adds the FluentValidation filter and declares the status it produces, as
    /// one call.
    /// <para>
    /// These were two independent calls, and an endpoint could carry the filter
    /// while documenting 400, or documenting nothing at all — three did. Binding
    /// them together means the published contract cannot disagree with the
    /// behaviour, which is a stronger guarantee than a test asserting they
    /// happen to match.
    /// </para>
    /// </summary>
    public static RouteHandlerBuilder WithValidation<TRequest>(this RouteHandlerBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder
            .AddEndpointFilter<ValidationFilter<TRequest>>()
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity);
    }

    public static IServiceCollection AddEndpoints(this IServiceCollection services)
    {
        services.AddEndpoints(Assembly.GetExecutingAssembly());
        return services;
    }

    public static IServiceCollection AddEndpoints(this IServiceCollection services, Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        ServiceDescriptor[] serviceDescriptors = assembly
            .DefinedTypes
            .Where(type => type is { IsAbstract: false, IsInterface: false } &&
                           type.IsAssignableTo(typeof(IEndpoint)))
            .Select(type => ServiceDescriptor.Transient(typeof(IEndpoint), type))
            .ToArray();

        services.TryAddEnumerable(serviceDescriptors);

        return services;
    }

    public static IApplicationBuilder MapFeatureEndpoints(this WebApplication app, RouteGroupBuilder? routeGroupBuilder = null)
    {
        ArgumentNullException.ThrowIfNull(app);
        IEnumerable<IEndpoint> endpoints = app.Services.GetRequiredService<IEnumerable<IEndpoint>>();
        IEndpointRouteBuilder builder = routeGroupBuilder is null ? app : routeGroupBuilder;

        foreach (IEndpoint endpoint in endpoints)
        {
            endpoint.MapEndpoint(builder);
        }

        return app;
    }
}
