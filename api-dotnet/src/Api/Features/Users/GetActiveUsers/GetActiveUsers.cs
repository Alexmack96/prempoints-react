using Api.Infrastructure.Endpoints;
using Api.Infrastructure.Endpoints.Filters;
using Ardalis.Result;
using Ardalis.Result.AspNetCore;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Api.Features.Users.GetActiveUsers;

public static class GetActiveUsers
{
    public record Query(DateOnly? AsAtDate) : IRequest<Result<List<UserDto>>>;
    public record Request(DateOnly? AsAtDate);
    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(team => team.AsAtDate).GreaterThanOrEqualTo(new DateOnly(2025, 1, 1));
        }
    }
    public class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("users/active", HandleAsync)
               .WithName("GetActiveUsers")
               .RequireRateLimiting("DefaultPolicy")
               .AddEndpointFilter<ValidationFilter<Request>>()
               .Produces<List<UserDto>>(StatusCodes.Status201Created)
               .ProducesValidationProblem()
               .ProducesProblem(StatusCodes.Status409Conflict)
               .ProducesProblem(StatusCodes.Status400BadRequest);
        }

        public static async Task<IResult> HandleAsync([AsParameters] Request request, [FromServices] ISender sender, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(sender);

            var query = new Query(request.AsAtDate);

            var result = await sender.Send(query, ct);

            return result.ToMinimalApiResult();
        }
    }
}