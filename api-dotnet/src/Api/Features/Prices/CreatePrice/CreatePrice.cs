using Api.Features.Teams;
using Api.Infrastructure.Endpoints;
using Api.Infrastructure.Endpoints.Filters;
using Ardalis.Result;
using Ardalis.Result.AspNetCore;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Api.Features.Prices.CreatePrice;

public static class CreatePrice
{
    public record Command(string TeamName, decimal Price, DateOnly ValueDate) : IRequest<Result<PriceDto>>;
    public record Request(string TeamName, decimal Price, DateOnly ValueDate);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(req => req.TeamName).NotEmpty();
            RuleFor(req => req.Price).GreaterThan(0);
            RuleFor(req => req.ValueDate).NotEmpty();
        }
    }
    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("prices", HandleAsync)
               .WithName("CreatePrice")
               .RequireRateLimiting("DefaultPolicy")
               .AddEndpointFilter<ValidationFilter<Request>>()
               .WithTags("Prices")
               .RequireAuthorization()
               .ProducesValidationProblem()
               .ProducesProblem(StatusCodes.Status401Unauthorized)
               .ProducesProblem(StatusCodes.Status403Forbidden)
               //Specific to the endpoint
               .ProducesProblem(StatusCodes.Status409Conflict)
               .Produces<TeamDto>(StatusCodes.Status201Created);
        }

        public static async Task<IResult> HandleAsync([FromBody] Request request, [FromServices] ISender sender, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(sender);
            ArgumentNullException.ThrowIfNull(request);

            var command = new Command(request.TeamName, request.Price, request.ValueDate);

            var result = await sender.Send(command, ct);

            return result.ToMinimalApiResult();
        }
    }
}
