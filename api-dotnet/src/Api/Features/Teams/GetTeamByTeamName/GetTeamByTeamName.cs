using Api.Infrastructure.Endpoints;
using Ardalis.Result;
using Ardalis.Result.AspNetCore;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Api.Features.Teams.GetTeamByTeamName;

public static class GetTeamByTeamName
{
    public record Query(string TeamName) : IRequest<Result<TeamDto>>;

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("teams/{teamName}", HandleAsync).WithTags("Teams");
        }

        public static async Task<IResult> HandleAsync([FromRoute] string teamName, [FromServices] ISender sender, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(sender);

            var query = new Query(teamName);

            var result = await sender.Send(query, ct);

            return result.ToMinimalApiResult();

        }
    }
}
