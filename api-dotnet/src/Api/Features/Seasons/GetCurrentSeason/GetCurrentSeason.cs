using Api.Infrastructure.Endpoints;
using Api.Infrastructure.Endpoints.Filters;
using Ardalis.Result;
using Ardalis.Result.AspNetCore;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Api.Features.Seasons.GetCurrentSeason;

public static class GetCurrentSeason
{
    /// <param name="AsAtDate">Optionally can choose a specific point in the past. Defaults to UTC Now if no date is provided.</param>
    public record Query(DateOnly? AsAtDate) : IRequest<Result<SeasonDto>>;
    public record Request(DateOnly? AsAtDate);

    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(r => r.AsAtDate).GreaterThanOrEqualTo(new DateOnly(2025, 1, 1));
        }
    }

    public class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("seasons/current", HandleAsync).AddEndpointFilter<ValidationFilter<Request>>();
        }

        public static async Task<IResult> HandleAsync(
            [AsParameters] Request request, [FromServices] ISender sender, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(sender);
            ArgumentNullException.ThrowIfNull(request);

            var query = new Query(request.AsAtDate);

            var result = await sender.Send(query, ct);

            return result.ToMinimalApiResult();
        }

    }
}
