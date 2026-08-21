using Api.Infrastructure.Endpoints;
using Ardalis.Result;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Api.Features.Trades.CreateTrades;

public static class CreateTrades
{
    public sealed record Request(
        DateTime TradeDateUtc,
        TradeType TradeType,
        string TimezoneIana,
        IReadOnlyDictionary<string, int> ExposuresByTeam
    );

    public sealed record Command(
        string WorkOsUserId,
        DateTime TradeDateUtc,
        TradeType TradeType,
        string TimezoneIana,
        IReadOnlyDictionary<string, int> ExposuresByTeam
    ) : IRequest<Result<List<TradeDto>>>;

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(req => req.TimezoneIana).NotEmpty();

            RuleFor(req => req.TradeType).IsInEnum();

            RuleFor(req => req.ExposuresByTeam)
                .NotEmpty()
                .WithMessage("Back at least one club.");

            RuleFor(req => req.ExposuresByTeam)
                .Must(exposures => exposures is null || exposures.Count <= TradingRules.MaxPositions)
                .WithMessage($"Back at most {TradingRules.MaxPositions} clubs.");

            // Each position on its own terms, before the total is considered —
            // "stake 7 on Arsenal" should say what is wrong with the 7, not
            // complain about a sum the player never intended.
            RuleFor(req => req.ExposuresByTeam)
                .Must(exposures => exposures is null || exposures.Values.All(IsValidStake))
                .WithMessage(
                    $"Each stake must be between {TradingRules.MinStake} and {TradingRules.TotalStake}, " +
                    $"in multiples of {TradingRules.StakeIncrement}. Short positions are negative.")
                .When(req => req.ExposuresByTeam is { Count: > 0 });

            // The rule that makes the board a decision: a player is placing
            // forty, and the only question is where.
            RuleFor(req => req.ExposuresByTeam)
                .Must(exposures => exposures is null || TotalStaked(exposures) == TradingRules.TotalStake)
                .WithMessage(exposures =>
                    $"Stakes must total exactly {TradingRules.TotalStake}. " +
                    $"This submission totals {TotalStaked(exposures.ExposuresByTeam)}.")
                .When(req => req.ExposuresByTeam is { Count: > 0 } && req.ExposuresByTeam.Values.All(IsValidStake));
        }

        /// Absolute, because a short is a position of the same size as a long.
        private static int TotalStaked(IReadOnlyDictionary<string, int> exposures) =>
            exposures.Values.Sum(Math.Abs);

        private static bool IsValidStake(int exposure)
        {
            var stake = Math.Abs(exposure);

            return stake >= TradingRules.MinStake
                && stake <= TradingRules.TotalStake
                && stake % TradingRules.StakeIncrement == 0;
        }
    }
    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            ArgumentNullException.ThrowIfNull(app);

            app.MapPost("trades", Handler)
               .WithName("CreateTrades")
               .WithTags("Trades")
               .WithSummary("Submit this week's trades for the signed-in player.")
               .WithValidation<Request>()
               // Identity comes from the token, never the body. This endpoint
               // used to take a username from the caller and require no
               // authentication at all, which let anyone submit trades as
               // anyone.
               .RequireAuthorization()
               .Produces<List<TradeDto>>(StatusCodes.Status200OK)
               .ProducesProblem(StatusCodes.Status401Unauthorized)
               .ProducesProblem(StatusCodes.Status403Forbidden)
               .ProducesProblem(StatusCodes.Status404NotFound);
        }

        public static async Task<IResult> Handler(
            [FromBody] Request request,
            ClaimsPrincipal user,
            [FromServices] ISender sender,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(sender);
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(user);

            var workOsUserId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(workOsUserId))
            {
                return Results.Unauthorized();
            }

            var command = new Command(
                workOsUserId,
                request.TradeDateUtc,
                request.TradeType,
                request.TimezoneIana,
                request.ExposuresByTeam);

            var response = await sender.Send(command, ct);

            return response.ToApiResult();
        }
    }
}
