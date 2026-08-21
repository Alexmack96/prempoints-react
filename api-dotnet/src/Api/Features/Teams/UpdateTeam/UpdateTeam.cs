using Api.Domain.Authorization;
using Api.Infrastructure.Endpoints;
using Api.Infrastructure.Endpoints.Filters;
using Ardalis.Result;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Api.Features.Teams.UpdateTeam;

/// <summary>
/// Full replacement, not a partial patch. A team has one mutable field, so
/// PATCH's null-versus-absent machinery would buy nothing, and PUT is
/// idempotent for free: sending the same body twice leaves the same state.
/// </summary>
public static class UpdateTeam
{
    public record Command(Guid Id, string TeamName) : IRequest<Result<TeamDto>>;
    public record Request(string TeamName);

    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(team => team.TeamName).NotEmpty().Length(1, 50);
        }
    }

    public class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            ArgumentNullException.ThrowIfNull(app);

            app.MapPut("teams/{id:guid}", HandleAsync)
               .WithName("UpdateTeam")
               .WithTags("Teams")
               .WithSummary("Replace a team's details.")
               .RequireRateLimiting("DefaultPolicy")
               .AddEndpointFilter<ValidationFilter<Request>>()
               .RequireAuthorization(Policies.Admin)
               .Produces<TeamDto>(StatusCodes.Status200OK)
               .ProducesValidationProblem()
               .ProducesProblem(StatusCodes.Status401Unauthorized)
               .ProducesProblem(StatusCodes.Status403Forbidden)
               .ProducesProblem(StatusCodes.Status404NotFound)
               .ProducesProblem(StatusCodes.Status409Conflict);
        }

        public static async Task<IResult> HandleAsync(
            [FromRoute] Guid id,
            [FromBody] Request request,
            [FromServices] ISender sender,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(sender);

            var result = await sender.Send(new Command(id, request.TeamName), ct);

            return result.ToApiResult();
        }
    }
}
