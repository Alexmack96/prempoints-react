using Api.Features.Teams;
using Api.Infrastructure.Endpoints;
using Api.Infrastructure.Endpoints.Filters;
using Ardalis.Result;
using Ardalis.Result.AspNetCore;
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
        List<string> RelegatedTeams) : IRequest<Result>;

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
            RuleFor(r => r.PromotedTeams).NotEmpty();
            RuleFor(r => r.RelegatedTeams).NotEmpty();
        }
    }

    public class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("seednewseason", HandleAsync)
               .WithName("SeedNewSeason")
               .WithValidation<Request>()
               .Produces(StatusCodes.Status200OK)
               .ProducesProblem(StatusCodes.Status409Conflict)
               .WithTags("Admin");
        }

        public static async Task<IResult> HandleAsync(
            [FromBody] Request request, [FromServices] IValidator<Request> validator, [FromServices] ISender sender, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(sender);
            ArgumentNullException.ThrowIfNull(request);

            var command = new Command(request.SeasonName, request.StartDate, request.EndDate, request.PromotedTeams, request.RelegatedTeams);

            var result = await sender.Send(command, ct);

            return result.ToMinimalApiResult();

        }

    }
}
