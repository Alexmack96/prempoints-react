using Api.Domain.Authorization;
using Api.Features.Teams;
using Api.Infrastructure.Endpoints;
using Api.Infrastructure.Endpoints.Filters;
using Ardalis.Result;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Api.Features.Prices.CreatePrice;

public static class CreatePrice
{
    public record Command(string TeamName, decimal Bid, decimal Ask, DateOnly ValueDate) : IRequest<Result<PriceDto>>;
    public record Request(string TeamName, decimal Bid, decimal Ask, DateOnly ValueDate);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(req => req.TeamName).NotEmpty();
            RuleFor(req => req.Bid).GreaterThan(0);
            RuleFor(req => req.Ask).GreaterThan(0);

            // A spread the wrong way round would make the mid meaningless and
            // let a player buy below the sell price.
            RuleFor(req => req.Ask)
                .GreaterThanOrEqualTo(req => req.Bid)
                .WithMessage("Ask must be greater than or equal to Bid.");
            RuleFor(req => req.ValueDate).NotEmpty();
        }
    }
    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("prices", HandleAsync)
               .WithName("CreatePrice")
               .WithValidation<Request>()
               .WithTags("Prices")
               // Admin, not merely signed in: a price is what every trade
               // settles against, so a player who could post one could move the
               // market they are trading. Matches prices/bulk beside it.
               .RequireAuthorization(Policies.Admin)
               .ProducesProblem(StatusCodes.Status401Unauthorized)
               .ProducesProblem(StatusCodes.Status403Forbidden)
               //Specific to the endpoint
               .ProducesProblem(StatusCodes.Status409Conflict)
               .Produces<PriceDto>(StatusCodes.Status200OK);
        }

        public static async Task<IResult> HandleAsync([FromBody] Request request, [FromServices] ISender sender, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(sender);
            ArgumentNullException.ThrowIfNull(request);

            var command = new Command(request.TeamName, request.Bid, request.Ask, request.ValueDate);

            var result = await sender.Send(command, ct);

            return result.ToApiResult();
        }
    }
}
