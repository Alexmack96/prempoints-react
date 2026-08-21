using Api.Infrastructure.Endpoints;
using Ardalis.Result;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Api.Features.Users.GetUserMe;

public static class GetUserMe
{
    // The Query requires the Auth/External ID, not the internal integer ID
    public record Query(string WorkOSUserId) : IRequest<Result<UserDto>>;

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            // Matches your frontend: apiClient.get('/users/me')
            app.MapGet("users/me", Handler)
               .WithName("GetUserMe")
               .Produces<UserDto>(StatusCodes.Status200OK)
               .ProducesProblem(StatusCodes.Status404NotFound)
               .WithTags("Users")
               .RequireAuthorization()
               .ProducesProblem(StatusCodes.Status401Unauthorized)
               .ProducesProblem(StatusCodes.Status403Forbidden); // Important: Ensure only logged-in users access this
        }

        public static async Task<IResult> Handler(
            ClaimsPrincipal user,
            [FromServices] ISender sender,
            CancellationToken ct = default)
        {
            // 1. Extract the ID from the JWT/Cookie (WorkOS usually uses NameIdentifier for the ID)
            var authId = user.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(authId))
            {
                return Results.Unauthorized();
            }

            var query = new Query(authId);

            var result = await sender.Send(query, ct);

            return result.ToApiResult();
        }
    }
}