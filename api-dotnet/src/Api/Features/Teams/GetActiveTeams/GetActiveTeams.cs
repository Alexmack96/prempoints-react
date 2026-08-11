using Api.Infrastructure.Endpoints;
using Api.Infrastructure.Endpoints.Filters;
using Ardalis.Result;
using Ardalis.Result.AspNetCore;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Api.Features.Teams.GetActiveTeams;

public static class GetActiveTeams
{
    public record Query(DateOnly? AsAtDate) : IRequest<Result<List<TeamDto>>>;
    public record Request(DateOnly? AsAtDate);

    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(req => req.AsAtDate)
                .GreaterThanOrEqualTo(new DateOnly(2025, 1, 1))
                .When(req => req.AsAtDate.HasValue);
        }
    }
    public class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("teams/active", HandleAsync)
             .WithName("GetActiveTeams")
             .RequireRateLimiting("DefaultPolicy")
             .AddEndpointFilter<ValidationFilter<Request>>()
             .Produces<List<TeamDto>>(StatusCodes.Status200OK)
             .ProducesValidationProblem()
             .ProducesProblem(StatusCodes.Status404NotFound);
        }

        public static async Task<IResult> HandleAsync([AsParameters] Request request, [FromServices] ISender sender, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(sender);
            ArgumentNullException.ThrowIfNull(request);

            var query = new Query(request.AsAtDate);

            var result = await sender.Send(query, ct);

            return result.ToMinimalApiResult();
        }
    }
}
