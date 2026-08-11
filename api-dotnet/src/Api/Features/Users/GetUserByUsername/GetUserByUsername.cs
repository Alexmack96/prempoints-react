using Api.Infrastructure.Endpoints;
using Ardalis.Result;
using Ardalis.Result.AspNetCore;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Api.Features.Users.GetUserByUsername;

public static class GetUserByUsername
{
    public record Query(string Username) : IRequest<Result<UserDto>>;
    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("users/{username}", Handler).WithTags("Users");
        }

        public static async Task<IResult> Handler([FromRoute] string username, [FromServices] ISender sender, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(sender);

            var query = new Query(username);

            var result = await sender.Send(query, ct);

            return result.ToMinimalApiResult();
        }
    }
}
