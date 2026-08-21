using Api.Infrastructure.Endpoints;
using Ardalis.Result;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Api.Features.Trades.GetJokerAllowance;

/// <summary>
/// Whether the signed-in player still has a joker for a given date.
/// <para>
/// Exists so the board can grey the checkbox out up front. Without it the only
/// way to discover the allowance is spent is to submit and be refused, which
/// tells a player they cannot do the thing only after they have decided to.
/// </para>
/// </summary>
public static class GetJokerAllowance
{
    public record Query(string WorkOsUserId, DateTime? TradeDateUtc)
        : IRequest<Result<JokerAllowanceDto>>;

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            ArgumentNullException.ThrowIfNull(app);

            app.MapGet("trades/joker-allowance", HandleAsync)
               .WithName("GetJokerAllowance")
               .WithTags("Trades")
               .WithSummary("Whether the signed-in player may play a joker on a date.")
               .RequireAuthorization()
               .Produces<JokerAllowanceDto>(StatusCodes.Status200OK)
               .ProducesProblem(StatusCodes.Status401Unauthorized)
               .ProducesProblem(StatusCodes.Status403Forbidden)
               .ProducesProblem(StatusCodes.Status404NotFound);
        }

        public static async Task<IResult> HandleAsync(
            [FromQuery] DateTime? tradeDateUtc,
            ClaimsPrincipal user,
            [FromServices] ISender sender,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(sender);
            ArgumentNullException.ThrowIfNull(user);

            var workOsUserId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(workOsUserId))
            {
                return Results.Unauthorized();
            }

            var result = await sender.Send(new Query(workOsUserId, tradeDateUtc), ct);

            return result.ToApiResult();
        }
    }
}
