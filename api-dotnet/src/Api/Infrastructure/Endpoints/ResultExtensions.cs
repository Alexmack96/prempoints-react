using Ardalis.Result;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Api.Infrastructure.Endpoints;

/// <summary>
/// Turns an <see cref="Result"/> into an HTTP response.
/// <para>
/// This replaces Ardalis' own <c>ToMinimalApiResult</c>. That helper renders a
/// failure's messages by concatenating them into a single string prefixed with
/// "Next error(s) occurred:*", which leaks its own formatting into the
/// <c>detail</c> field of every error body we publish. Since the error contract
/// is part of the API, it is written here rather than inherited.
/// </para>
/// </summary>
public static class ResultExtensions
{
    public static IResult ToApiResult<T>(this Result<T> result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.Status == ResultStatus.Ok
            ? Results.Ok(result.Value)
            : Problem(result.Status, result.Errors, result.ValidationErrors);
    }

    /// <summary>
    /// Maps success to <c>204 No Content</c>. A delete that succeeded has
    /// nothing to say, and a 200 with an empty body invites clients to parse one.
    /// </summary>
    public static IResult ToNoContentApiResult(this Result result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.Status == ResultStatus.Ok
            ? Results.NoContent()
            : Problem(result.Status, result.Errors, result.ValidationErrors);
    }

    /// <summary>
    /// Maps success to <c>201 Created</c> with a <c>Location</c> header.
    /// <para>
    /// The location is resolved from the item endpoint's route name via
    /// <see cref="LinkGenerator"/> rather than a hand-built string, so the
    /// <c>/api/v1</c> prefix stays in Program.cs and cannot drift out of sync
    /// with the routes it prefixes.
    /// </para>
    /// </summary>
    public static IResult ToCreatedApiResult<T>(
        this Result<T> result,
        HttpContext httpContext,
        string routeName,
        Func<T, object> routeValues)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(routeValues);

        if (result.Status != ResultStatus.Ok)
        {
            return Problem(result.Status, result.Errors, result.ValidationErrors);
        }

        var linkGenerator = httpContext.RequestServices.GetRequiredService<LinkGenerator>();
        var location = linkGenerator.GetPathByName(httpContext, routeName, routeValues(result.Value));

        return Results.Created(location, result.Value);
    }

    private static IResult Problem(
        ResultStatus status,
        IEnumerable<string> errors,
        IEnumerable<ValidationError> validationErrors)
    {
        var detail = string.Join(" ", errors ?? []);

        if (status == ResultStatus.Invalid)
        {
            var grouped = validationErrors
                .GroupBy(e => e.Identifier, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray(), StringComparer.Ordinal);

            return ApiProblem.Validation(grouped);
        }

        var (statusCode, title, type) = Describe(status);

        return Results.Problem(
            detail: string.IsNullOrWhiteSpace(detail) ? null : detail,
            statusCode: statusCode,
            title: title,
            type: type);
    }

    private static (int StatusCode, string Title, string Type) Describe(ResultStatus status) => status switch
    {
        ResultStatus.NotFound => (StatusCodes.Status404NotFound, "Not Found", ProblemTypes.NotFound),
        ResultStatus.Conflict => (StatusCodes.Status409Conflict, "Conflict", ProblemTypes.Conflict),
        ResultStatus.Forbidden => (StatusCodes.Status403Forbidden, "Forbidden", ProblemTypes.Forbidden),
        ResultStatus.Unauthorized => (StatusCodes.Status401Unauthorized, "Unauthorized", ProblemTypes.Unauthorized),
        ResultStatus.Unavailable => (StatusCodes.Status503ServiceUnavailable, "Service Unavailable", ProblemTypes.Unavailable),
        _ => (StatusCodes.Status500InternalServerError, "Internal Server Error", ProblemTypes.ServerError),
    };
}

/// <summary>
/// The one place a validation failure becomes a response.
/// <para>
/// Both the FluentValidation endpoint filter and the Result mapping above end
/// up here. They used to build their own, and agreed on the status code and
/// type URI only by coincidence — one edit away from an API that answers the
/// same failure two different ways depending on which layer caught it.
/// </para>
/// </summary>
public static class ApiProblem
{
    public static IResult Validation(IDictionary<string, string[]> errors) =>
        Results.ValidationProblem(
            errors,
            statusCode: StatusCodes.Status422UnprocessableEntity,
            type: ProblemTypes.UnprocessableEntity);
}

/// <summary>
/// The <c>type</c> URIs our problem responses use. Pointing at the RFC that
/// defines each status keeps them stable and dereferenceable, which is the
/// whole point of the field — a client can look one up rather than
/// pattern-matching on English titles.
/// </summary>
public static class ProblemTypes
{
    private const string Rfc9110 = "https://tools.ietf.org/html/rfc9110#section-";

    public const string Unauthorized = Rfc9110 + "15.5.2";
    public const string Forbidden = Rfc9110 + "15.5.4";
    public const string NotFound = Rfc9110 + "15.5.5";
    public const string Conflict = Rfc9110 + "15.5.10";
    public const string UnprocessableEntity = "https://tools.ietf.org/html/rfc4918#section-11.2";
    public const string ServerError = Rfc9110 + "15.6.1";
    public const string Unavailable = Rfc9110 + "15.6.4";
}
