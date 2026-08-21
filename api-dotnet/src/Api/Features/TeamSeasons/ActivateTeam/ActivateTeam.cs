using Api.Domain.Authorization;
using Api.Infrastructure.Endpoints;
using Api.Infrastructure.Endpoints.Filters;
using Ardalis.Result;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Api.Features.TeamSeasons.ActivateTeam;

public static class ActivateTeam
{
    public record Command(string TeamName, DateOnly? AsAtDate) : IRequest<Result<TeamSeasonDto>>;
    public record Request(DateOnly? AsAtDate);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(req => req.AsAtDate).GreaterThanOrEqualTo(new DateOnly(2025, 1, 1));
        }
    }
    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("teams/activate/{teamName}", Handler)
               .WithName("ActivateTeam")
               .Produces<TeamSeasonDto>(StatusCodes.Status200OK)
               .ProducesProblem(StatusCodes.Status404NotFound)
               // Enrols a club in a season, which puts it on everyone's board.
               .RequireAuthorization(Policies.Admin)
               .ProducesProblem(StatusCodes.Status401Unauthorized)
               .ProducesProblem(StatusCodes.Status403Forbidden)
               .WithTags("TeamSeasons").WithValidation<Request>();
        }

        public static async Task<IResult> Handler([FromRoute] string teamName, [FromBody] Request request, [FromServices] ISender sender)
        {
            ArgumentNullException.ThrowIfNull(sender);
            ArgumentNullException.ThrowIfNull(request);

            var command = new Command(teamName, request.AsAtDate);

            var result = await sender.Send(command);

            return result.ToApiResult();
        }
    }
}
