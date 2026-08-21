using Api.Infrastructure.Endpoints;
using Ardalis.Result;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Api.Features.Teams.GetTeamById;

/// <summary>
/// The canonical item route. Identity is the opaque id, never the team name:
/// a name is mutable and user-supplied, so a URL built from it breaks on the
/// first rename and collides with literal segments like <c>/teams/active</c>.
/// Look a team up by name through <c>GET /teams?name=</c> instead.
/// </summary>
public static class GetTeamById
{
    public record Query(Guid Id) : IRequest<Result<TeamDto>>;

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            ArgumentNullException.ThrowIfNull(app);

            app.MapGet("teams/{id:guid}", HandleAsync)
               .WithName("GetTeamById")
               .WithTags("Teams")
               .WithSummary("Fetch a single team by its identifier.")
               .RequireRateLimiting("DefaultPolicy")
               .Produces<TeamDto>(StatusCodes.Status200OK)
               .ProducesProblem(StatusCodes.Status404NotFound);
        }

        public static async Task<IResult> HandleAsync(
            [FromRoute] Guid id,
            [FromServices] ISender sender,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(sender);

            var result = await sender.Send(new Query(id), ct);

            return result.ToApiResult();
        }
    }
}
