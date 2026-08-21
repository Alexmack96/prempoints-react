using Api.Domain.Authorization;
using Api.Features.Teams;
using Api.Infrastructure.Endpoints;
using Api.Infrastructure.Endpoints.Filters;
using Ardalis.Result;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Api.Features.Admin.SeedNewSeason;

public static class SeedNewSeason
{
    public record Command(
        string SeasonName,
        DateOnly StartDate,
        DateOnly EndDate,
        List<string> PromotedTeams,
        List<string> RelegatedTeams) : IRequest<Result<SeedNewSeasonResult>>;

    public record Request(
        string SeasonName,
        DateOnly StartDate,
        DateOnly EndDate,
        List<string> PromotedTeams,
        List<string> RelegatedTeams);

    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(r => r.SeasonName).NotEmpty().Length(1, 50);

            // Neither list is required. Seeding the first ever season has
            // nothing to relegate, and passing the whole league as "promoted"
            // is how an empty database gets its twenty clubs. The handler
            // decides whether the resulting roster makes sense.
            RuleFor(r => r.PromotedTeams).NotNull();
            RuleFor(r => r.RelegatedTeams).NotNull();
        }
    }

    public class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("seednewseason", HandleAsync)
               .WithName("SeedNewSeason")
               .WithValidation<Request>()
               .Produces<SeedNewSeasonResult>(StatusCodes.Status200OK)
               .ProducesProblem(StatusCodes.Status409Conflict)
               // Reseeds an entire season: every team enrolment and gameweek in it.
               .RequireAuthorization(Policies.Admin)
               .ProducesProblem(StatusCodes.Status401Unauthorized)
               .ProducesProblem(StatusCodes.Status403Forbidden)
               .WithTags("Admin");
        }

        public static async Task<IResult> HandleAsync(
            [FromBody] Request request, [FromServices] IValidator<Request> validator, [FromServices] ISender sender, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(sender);
            ArgumentNullException.ThrowIfNull(request);

            var command = new Command(request.SeasonName, request.StartDate, request.EndDate, request.PromotedTeams, request.RelegatedTeams);

            var result = await sender.Send(command, ct);

            return result.ToApiResult();

        }

    }
}
