using Api.Infrastructure.Endpoints;
using Api.Infrastructure.Endpoints.Filters;
using Ardalis.Result;
using Ardalis.Result.AspNetCore;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Api.Features.Teams.CreateTeam;

public static class CreateTeam
{
    public record Command(string TeamName) : IRequest<Result<TeamDto>>;
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
            app.MapPost("teams", HandleAsync)
               .WithName("CreateTeam")
               .RequireRateLimiting("DefaultPolicy")
               .AddEndpointFilter<ValidationFilter<Request>>()
               .WithTags("Teams")
               .RequireAuthorization()
               .ProducesValidationProblem()
               .ProducesProblem(StatusCodes.Status401Unauthorized)
               .ProducesProblem(StatusCodes.Status403Forbidden)
               //Specific to the endpoint
               .ProducesProblem(StatusCodes.Status409Conflict)
               .Produces<TeamDto>(StatusCodes.Status201Created);
        }

        public static async Task<IResult> HandleAsync(
            [FromBody] Request request,
            ClaimsPrincipal user,
            [FromServices] ISender sender,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(user);
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(sender);

            var workOSUserId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(workOSUserId))
                return Result.Forbidden("User identity could not be verified.").ToMinimalApiResult();

            var command = new Command(request.TeamName);
            var result = await sender.Send(command, ct);
            return result.ToMinimalApiResult();
        }
    }
}