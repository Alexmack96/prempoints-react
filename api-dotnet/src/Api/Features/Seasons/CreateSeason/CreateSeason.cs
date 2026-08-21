using Api.Domain.Authorization;
using Api.Infrastructure.Endpoints;
using Ardalis.Result;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Api.Features.Seasons.CreateSeason;

public static class CreateSeason
{
    public record Command(string SeasonName, int StartYear) : IRequest<Result<SeasonDto>>;
    public record Request(string SeasonName, int StartYear);

    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(r => r.SeasonName).NotEmpty().Length(1, 50);
        }
    }

    public class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("seasons", HandleAsync)
               .WithName("CreateSeason")
               .Produces<SeasonDto>(StatusCodes.Status200OK)
               .ProducesProblem(StatusCodes.Status409Conflict)
               // A season is the container every trade, price and standing hangs off.
               .RequireAuthorization(Policies.Admin)
               .ProducesProblem(StatusCodes.Status401Unauthorized)
               .ProducesProblem(StatusCodes.Status403Forbidden)
               .WithTags("Seasons");
        }

        public static async Task<IResult> HandleAsync([FromBody] Request request, [FromServices] ISender sender, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(sender);
            ArgumentNullException.ThrowIfNull(request);

            var command = new Command(request.SeasonName, request.StartYear);
            var result = await sender.Send(command, ct);

            if (result.IsSuccess)
            {
                return Results.Created($"/seasons/{result.Value.Id}", result.Value);
            }

            return result.ToApiResult();
        }
    }
}
