using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace Api.Infrastructure.Middleware;

/// <summary>
/// The last line of defence. Expected failures are values — handlers return
/// <c>Result.NotFound</c>, <c>Result.Conflict</c> and friends — so anything
/// arriving here is a panic, with one deliberate exception: a unique-index
/// violation that beat a handler's own check in a race.
/// </summary>
public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    /// <summary>
    /// SQL Server's unique-constraint and unique-index violations.
    /// <para>
    /// Matched on error number rather than by searching the message for "IX_",
    /// which the previous version did. That test was both too broad — any
    /// exception whose text happened to contain those two letters became a 409 —
    /// and too narrow, since it missed primary-key violations and any index not
    /// following that naming convention. It also depended on the server's
    /// message language.
    /// </para>
    /// </summary>
    private const int DuplicateKeyError = 2601;
    private const int UniqueConstraintError = 2627;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(exception);

        var traceId = httpContext.TraceIdentifier;

        var problemDetails = IsUniqueConstraintViolation(exception)
            ? Conflict()
            : ServerError();

        // A conflict is the client's problem and is expected under a race, so it
        // logs as a warning. Anything else is ours, and gets the stack trace.
        if (problemDetails.Status == StatusCodes.Status409Conflict)
        {
            logger.LogWarning(
                "Unique constraint violated on {Path}. TraceId: {TraceId}",
                httpContext.Request.Path,
                traceId);
        }
        else
        {
            logger.LogError(
                exception,
                "Could not process a request on Machine {Machine}. TraceId: {TraceId}",
                Environment.MachineName,
                traceId);
        }

        problemDetails.Instance = httpContext.Request.Path;
        problemDetails.Extensions["traceId"] = traceId;

        httpContext.Response.StatusCode = problemDetails.Status!.Value;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }

    private static ProblemDetails Conflict() => new()
    {
        Status = StatusCodes.Status409Conflict,
        Title = "Conflict",
        Detail = "A record with the same unique key already exists.",
        Type = "https://tools.ietf.org/html/rfc9110#section-15.5.10",
    };

    private static ProblemDetails ServerError() => new()
    {
        Status = StatusCodes.Status500InternalServerError,
        Title = "Internal Server Error",
        Detail = "An unexpected error occurred. Please contact support.",
        Type = "https://tools.ietf.org/html/rfc9110#section-15.6.1",
    };

    private static bool IsUniqueConstraintViolation(Exception ex)
    {
        // Walks the chain because EF wraps the provider exception in a
        // DbUpdateException, and a retrying execution strategy can wrap that
        // again.
        for (var current = ex; current is not null; current = current.InnerException)
        {
            if (current is SqlException sql &&
                sql.Errors.Cast<SqlError>().Any(e =>
                    e.Number is DuplicateKeyError or UniqueConstraintError))
            {
                return true;
            }
        }

        return false;
    }
}
