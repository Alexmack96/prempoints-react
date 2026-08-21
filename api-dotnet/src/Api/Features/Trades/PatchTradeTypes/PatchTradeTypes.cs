using Api.Domain.Authorization;
using Api.Infrastructure.Endpoints;
using Api.Infrastructure.Endpoints.Filters;
using Ardalis.Result;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Api.Features.Trades.PatchTradeTypes;

public static class PatchTradeTypes
{
    public record Command(List<Guid> TradeIds, TradeType TradeType) : IRequest<Result<List<TradeDto>>>;
    public record Request(List<Guid> TradeIds, TradeType TradeType);
    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(r => r.TradeType).IsInEnum();

            RuleFor(r => r.TradeIds)
              .NotEmpty()
              .WithMessage("At least one trade ID must be supplied.");

            RuleForEach(r => r.TradeIds)
                .NotEmpty()
                .WithMessage("Trade ID cannot be empty.");
        }
    }

    public class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPatch("trades/type", HandleAsync)
               .WithTags("Trades")
               .WithName("PatchTradeTypes")
               .Produces<List<TradeDto>>(StatusCodes.Status200OK)
               .ProducesProblem(StatusCodes.Status404NotFound)
               // Bulk-reclassifies trades, and a Joker doubles what one pays out.
               .RequireAuthorization(Policies.Admin)
               .ProducesProblem(StatusCodes.Status401Unauthorized)
               .ProducesProblem(StatusCodes.Status403Forbidden)
               .WithValidation<Request>();
        }

        public static async Task<IResult> HandleAsync(
            [FromBody] Request request, [FromServices] ISender sender, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(sender);
            ArgumentNullException.ThrowIfNull(request);

            var command = new Command(request.TradeIds, request.TradeType);

            var result = await sender.Send(command, ct);

            return result.ToApiResult();
        }
    }
}
