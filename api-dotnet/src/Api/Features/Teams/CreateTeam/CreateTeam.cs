using Api.Domain.Authorization;
using Api.Infrastructure.Endpoints;
using Api.Infrastructure.Endpoints.Filters;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Api.Features.Teams.CreateTeam;

public static class CreateTeam
{
    public record Command(string TeamName) : IRequest<Ardalis.Result.Result<TeamDto>>;
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

            app.MapPost("teams", HandleAsync)
               .WithName("CreateTeam")
               .WithTags("Teams")
               .WithSummary("Create a team and enrol it in the current season.")
               .RequireRateLimiting("DefaultPolicy")
               .AddEndpointFilter<ValidationFilter<Request>>()
               // Teams are reference data. Any signed-in player could otherwise
               // add one, and every price and trade in the game hangs off them.
               .RequireAuthorization(Policies.Admin)
               .Produces<TeamDto>(StatusCodes.Status201Created)
               .ProducesValidationProblem()
               .ProducesProblem(StatusCodes.Status401Unauthorized)
               .ProducesProblem(StatusCodes.Status403Forbidden)
               .ProducesProblem(StatusCodes.Status404NotFound)
               .ProducesProblem(StatusCodes.Status409Conflict);
        }

        public static async Task<IResult> HandleAsync(
            [FromBody] Request request,
            HttpContext httpContext,
            [FromServices] ISender sender,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(sender);

            // No manual claim inspection here any more: Policies.Admin already
            // rejected anyone without an identity, so a hand-rolled check could
            // only ever disagree with the policy.
            var result = await sender.Send(new Command(request.TeamName), ct);

            return result.ToCreatedApiResult(
                httpContext,
                routeName: "GetTeamById",
                routeValues: dto => new { id = dto.Id });
        }
    }
}
