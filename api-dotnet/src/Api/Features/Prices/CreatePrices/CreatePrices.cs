using Api.Domain.Authorization;
using Api.Infrastructure.Endpoints;
using Ardalis.Result;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Api.Features.Prices.CreatePrices;

/// <summary>
/// Loads a whole gameweek's prices in one call.
/// <para>
/// A task-shaped command rather than a collection POST, in the same style as
/// PatchTradeTypes: it upserts many rows against one value date and returns
/// them together. The single-price endpoint stays for corrections; this exists
/// because the real operation is "here is the board for this week", and doing
/// that twenty requests at a time through Swagger is how a club gets missed.
/// </para>
/// </summary>
public static class CreatePrices
{
    public record Spread(string TeamName, decimal Bid, decimal Ask);

    public record Command(DateOnly ValueDate, IReadOnlyList<Spread> Prices)
        : IRequest<Result<List<PriceDto>>>;

    public record Request(DateOnly ValueDate, List<Spread> Prices);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(request => request.ValueDate).NotEmpty();

            RuleFor(request => request.Prices)
                .NotEmpty()
                .WithMessage("At least one price must be supplied.");

            RuleForEach(request => request.Prices).ChildRules(price =>
            {
                price.RuleFor(p => p.TeamName).NotEmpty();
                price.RuleFor(p => p.Bid).GreaterThan(0);
                price.RuleFor(p => p.Ask).GreaterThan(0);
                price.RuleFor(p => p.Ask)
                     .GreaterThanOrEqualTo(p => p.Bid)
                     .WithMessage("Ask must be greater than or equal to Bid.");
            });
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            ArgumentNullException.ThrowIfNull(app);

            app.MapPost("prices/bulk", HandleAsync)
               .WithName("CreatePrices")
               .WithTags("Prices")
               .WithSummary("Upsert every price for a value date in one call.")
               .WithValidation<Request>()
               // Prices decide what every trade settles at, so this is reference
               // data in the same sense teams are.
               .RequireAuthorization(Policies.Admin)
               .Produces<List<PriceDto>>(StatusCodes.Status200OK)
               .ProducesProblem(StatusCodes.Status401Unauthorized)
               .ProducesProblem(StatusCodes.Status403Forbidden)
               .ProducesProblem(StatusCodes.Status404NotFound);
        }

        public static async Task<IResult> HandleAsync(
            [FromBody] Request request,
            [FromServices] ISender sender,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(sender);

            var result = await sender.Send(new Command(request.ValueDate, request.Prices), ct);

            return result.ToApiResult();
        }
    }
}
