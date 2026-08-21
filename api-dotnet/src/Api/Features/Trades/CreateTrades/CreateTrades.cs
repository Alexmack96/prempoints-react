using Api.Infrastructure.Endpoints;
using Ardalis.Result;
using Ardalis.Result.AspNetCore;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Api.Features.Trades.CreateTrades;

public static class CreateTrades
{
    public sealed record Request(
        string Username,
        DateTime TradeDateUtc,
        TradeType TradeType,
        string TimezoneIana,
        IReadOnlyDictionary<string, int> ExposuresByTeam
    );

    public sealed record Command(
        string Username,
        DateTime TradeDateUtc,
        TradeType TradeType,
        string TimezoneIana,
        IReadOnlyDictionary<string, int> ExposuresByTeam
    ) : IRequest<Result<List<TradeDto>>>;

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(req => req.Username).NotEmpty().Length(1, 50);
        }
    }
    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("trades", Handler)
               .WithName("CreateTrades")
               .Produces<List<TradeDto>>(StatusCodes.Status200OK)
               .ProducesProblem(StatusCodes.Status404NotFound).WithTags("Trades");
        }

        public static async Task<IResult> Handler([FromBody] Request request, [FromServices] ISender sender, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(sender);
            ArgumentNullException.ThrowIfNull(request);

            var command = new Command(request.Username, request.TradeDateUtc, request.TradeType, request.TimezoneIana, request.ExposuresByTeam);
            var response = await sender.Send(command, ct);

            return response.ToMinimalApiResult();
        }
    }
}
