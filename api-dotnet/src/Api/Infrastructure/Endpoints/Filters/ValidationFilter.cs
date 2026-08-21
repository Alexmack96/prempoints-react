using Api.Infrastructure.Endpoints;
using FluentValidation;

namespace Api.Infrastructure.Endpoints.Filters;

public class ValidationFilter<T>(ILogger<ValidationFilter<T>> logger) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var validator = context.HttpContext.RequestServices.GetService<IValidator<T>>();
        if (validator is null)
            return await next(context);

        var request = context.Arguments.OfType<T>().FirstOrDefault();
        if (request is null)
        {
            logger.LogError("Validation filter applied but type {Type} was not found in arguments.", typeof(T).Name);
            return Results.Problem("Internal Server Error: Validation configuration failure.");
        }

        var validation = await validator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            var errors = validation.ToDictionary();

            // Log as Warning: Client error. logging Keys ensures we don't log sensitive PII values.
            logger.LogWarning("Validation failed for {Type}. Failed Fields: {@FailedFields}", typeof(T).Name, errors.Keys);

            return ApiProblem.Validation(errors);
        }

        return await next(context);
    }
}