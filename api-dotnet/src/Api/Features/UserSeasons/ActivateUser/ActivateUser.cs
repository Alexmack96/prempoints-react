using Api.Domain.Authorization;
using Api.Infrastructure.Endpoints;
using Api.Infrastructure.Endpoints.Filters;
using Ardalis.Result;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Api.Features.UserSeasons.ActivateUser;

public static class ActivateUser
{
    public record Command(string Username, DateOnly? AsAtDate, int LateJoinerFee) : IRequest<Result<UserSeasonDto>>;
    public record Request(DateOnly? AsAtDate, int LateJoinerFee);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(req => req.LateJoinerFee).GreaterThan(0);
            RuleFor(req => req.AsAtDate).GreaterThanOrEqualTo(new DateOnly(2025, 1, 1));
        }
    }
    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("users/activate/{username}", HandleAsync)
               .WithName("ActivateUser")
               .Produces<UserSeasonDto>(StatusCodes.Status200OK)
               .ProducesProblem(StatusCodes.Status404NotFound)
               // Enrols another player and sets their late-joiner fee.
               .RequireAuthorization(Policies.Admin)
               .ProducesProblem(StatusCodes.Status401Unauthorized)
               .ProducesProblem(StatusCodes.Status403Forbidden)
               .WithTags("UserSeasons").WithValidation<Request>();
        }

        public static async Task<IResult> HandleAsync(
            [FromRoute] string username, [FromBody] Request request, [FromServices] ISender sender, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(sender);
            ArgumentNullException.ThrowIfNull(request);

            var command = new Command(username, request.AsAtDate, request.LateJoinerFee);

            var result = await sender.Send(command, ct);

            return result.ToApiResult();
        }
    }
}
