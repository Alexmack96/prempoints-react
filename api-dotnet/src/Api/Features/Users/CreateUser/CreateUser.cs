using Api.Domain.Authorization;
using Api.Features.Teams;
using Api.Infrastructure.Endpoints;
using Api.Infrastructure.Endpoints.Filters;
using Ardalis.Result;
using Ardalis.Result.AspNetCore;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Api.Features.Users.CreateUser;

public static class CreateUser
{
    public record Command(string WorkOSUserId, string Username, string FirstName, string LastName, string Email, UserRole Role) : IRequest<Result<UserDto>>;
    public record Request(string Username, string FirstName, string LastName, string Email, UserRole Role);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(req => req.Username).NotEmpty().Length(1, 50);
            RuleFor(req => req.FirstName).NotEmpty().Length(1, 50);
            RuleFor(req => req.LastName).NotEmpty().Length(1, 50);
            RuleFor(req => req.Email).NotEmpty().Length(1, 50);
            RuleFor(req => req.Role).IsInEnum();
        }
    }
    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("users", Handler).WithTags("Users")
               .WithName("CreateUser")
               .RequireRateLimiting("DefaultPolicy")
               .AddEndpointFilter<ValidationFilter<Request>>()
               .Produces<TeamDto>(StatusCodes.Status201Created)
               .ProducesValidationProblem()
               .ProducesProblem(StatusCodes.Status409Conflict)
               .ProducesProblem(StatusCodes.Status400BadRequest)
               .WithTags("Users")
               .RequireAuthorization();
        }

        public static async Task<IResult> Handler([FromBody] Request request, ClaimsPrincipal user, [FromServices] ISender sender, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(sender);
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(user);

            var workOSUserId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(workOSUserId))
                return Result.Forbidden("User identity could not be verified.").ToMinimalApiResult();

            var command = new Command(workOSUserId, request.Username, request.FirstName, request.LastName, request.Email, request.Role);
            var result = await sender.Send(command, ct);

            return result.ToMinimalApiResult();
        }
    }
}
