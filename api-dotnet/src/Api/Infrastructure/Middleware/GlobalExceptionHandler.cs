using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Infrastructure.Middleware;

/// <summary>
/// This really should only handle panics since we expect results pattern to handle all 'expected' failures.
/// </summary>
/// <param name="logger"></param>
public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(exception);

        var traceId = httpContext.TraceIdentifier;

        logger.LogError(
                exception,
                "Could not process a request on Machine {Machine}. TraceId: {TraceId}",
                Environment.MachineName,
                traceId
            );

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Internal Server Error",
            Detail = "An unexpected error occurred. Please contact support.",
            Instance = httpContext.Request.Path
        };

        problemDetails.Extensions.Add("traceId", traceId);

        if (IsUniqueConstraintViolation(exception))
        {
            problemDetails.Status = StatusCodes.Status409Conflict;
            problemDetails.Title = "Conflict";
            problemDetails.Detail = "A record with the same unique key already exists.";
        }

        httpContext.Response.StatusCode = problemDetails.Status.Value;

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }

    private static bool IsUniqueConstraintViolation(Exception ex)
    {
        return ex.InnerException?.Message.Contains("IX_", StringComparison.OrdinalIgnoreCase) == true;
    }
}