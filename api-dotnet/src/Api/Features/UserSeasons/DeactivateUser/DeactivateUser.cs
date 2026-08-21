using Api.Domain.Authorization;
using Api.Infrastructure.Endpoints;
using Api.Infrastructure.Endpoints.Filters;
using Ardalis.Result;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Api.Features.UserSeasons.DeactivateUser;

public static class DeactivateUser
{
    public record Command(string Username, DateOnly? AsAtDate) : IRequest<Result>;
    public record Request(DateOnly? AsAtDate);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(req => req.AsAtDate).GreaterThan(new DateOnly(2025, 1, 1));
        }
    }
    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("users/deactivate/{username}", HandleAsync)
               .WithName("DeactivateUser")
               .Produces(StatusCodes.Status200OK)
               .ProducesProblem(StatusCodes.Status404NotFound)
               // Removes another player from the season.
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

            var command = new Command(username, request.AsAtDate);

            var response = await sender.Send(command, ct);

            return response.ToApiResult();
        }
    }
}
