using Api.Features.GetPnlDetails;
using Api.Features.Teams;
using Api.Infrastructure.Endpoints;
using Api.Infrastructure.Endpoints.Filters;
using Ardalis.Result;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Api.Features.GetPnlByTrade;

public static class GetPnlByTrade
{
    public record Query(string? Username, DateOnly? AsAtDate) : IRequest<Result<List<PnlByTrade>>>;
    public record Request(DateOnly? AsAtDate);
    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(req => req.AsAtDate)
                .GreaterThanOrEqualTo(new DateOnly(2025, 1, 1))
                .When(req => req.AsAtDate.HasValue);
        }
    }
    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("pnl/trade/{username?}", HandleAsync)
               .WithTags("Pnl")
                .WithName("GetPnlByTrade")
                .WithValidation<Request>()
                .Produces<List<PnlByTrade>>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status404NotFound);
        }

        public static async Task<IResult> HandleAsync(
            [FromRoute] string? username,
            [AsParameters] Request request,
            [FromServices] ISender sender,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(sender);
            ArgumentNullException.ThrowIfNull(request);

            var query = new Query(username, request.AsAtDate);

            var result = await sender.Send(query, ct);

            return result.ToApiResult();
        }
    }
}