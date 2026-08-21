using Api.Domain.Authorization;
using Api.Infrastructure.Endpoints;
using Ardalis.Result;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Api.Features.Teams.DeleteTeam;

/// <summary>
/// A real delete, not a soft one. There is no "deleted team" concept in this
/// game — a team added in error is removed and replaced. A team that anything
/// already references is refused with 409 rather than cascaded, because
/// cascading would silently destroy the prices and trades that make up a
/// player's position.
/// </summary>
public static class DeleteTeam
{
    public record Command(Guid Id) : IRequest<Result>;

    public class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            ArgumentNullException.ThrowIfNull(app);

            app.MapDelete("teams/{id:guid}", HandleAsync)
               .WithName("DeleteTeam")
               .WithTags("Teams")
               .WithSummary("Delete a team that nothing references.")
               .RequireAuthorization(Policies.Admin)
               .Produces(StatusCodes.Status204NoContent)
               .ProducesProblem(StatusCodes.Status401Unauthorized)
               .ProducesProblem(StatusCodes.Status403Forbidden)
               .ProducesProblem(StatusCodes.Status404NotFound)
               .ProducesProblem(StatusCodes.Status409Conflict);
        }

        public static async Task<IResult> HandleAsync(
            [FromRoute] Guid id,
            [FromServices] ISender sender,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(sender);

            var result = await sender.Send(new Command(id), ct);

            return result.ToNoContentApiResult();
        }
    }
}
