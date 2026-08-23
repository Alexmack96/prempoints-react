using Api.Infrastructure.Endpoints;
using Api.Infrastructure.Endpoints.Filters;
using Ardalis.Result;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Api.Features.Users.UpdateMyProfile;

/// <summary>
/// The two things a player chooses about themselves: what they are called and
/// which club's badge sits next to their name.
/// <para>
/// One call rather than two, because the onboarding gate asks both on one
/// screen and submitting them separately would let someone end up named but
/// badgeless if the second request failed.
/// </para>
/// <para>
/// It edits the caller and only the caller. The internal id comes from the
/// principal, never from the body, so there is no id to tamper with and no
/// admin policy needed to protect other people's rows.
/// </para>
/// </summary>
public static class UpdateMyProfile
{
    public record Command(Guid UserId, string Username, Guid? FavouriteTeamId) : IRequest<Result<UserDto>>;

    public record Request(string Username, Guid? FavouriteTeamId);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(request => request.Username)
                .NotEmpty()
                .Length(3, 50)
                // Shown on the leaderboard next to a badge, so it stays to
                // characters that render predictably at small sizes and cannot
                // be confused for markup. Generated names are letters and
                // digits only, so any of those already passes.
                .Matches("^[A-Za-z0-9_-]+$")
                .WithMessage("Username may use letters, digits, hyphens and underscores.");
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            ArgumentNullException.ThrowIfNull(app);

            app.MapPatch("users/me", HandleAsync)
               .WithName("UpdateMyProfile")
               .WithTags("Users")
               .WithSummary("Set the signed-in player's username and favourite club.")
               .WithValidation<Request>()
               .RequireAuthorization()
               .Produces<UserDto>(StatusCodes.Status200OK)
               .ProducesProblem(StatusCodes.Status401Unauthorized)
               .ProducesProblem(StatusCodes.Status403Forbidden)
               .ProducesProblem(StatusCodes.Status404NotFound)
               .ProducesProblem(StatusCodes.Status409Conflict);
        }

        public static async Task<IResult> HandleAsync(
            [FromBody] Request request,
            ClaimsPrincipal user,
            [FromServices] ISender sender,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(user);
            ArgumentNullException.ThrowIfNull(sender);

            // Present only once UserProvisioner has matched the token to a row,
            // so its absence means authenticated-but-not-a-player rather than
            // an authorization failure.
            var internalUserId = user.FindFirst("InternalUserId")?.Value;

            if (!Guid.TryParse(internalUserId, out var userId))
            {
                return Result.NotFound("No PremPoints player is linked to this sign-in.").ToApiResult();
            }

            var result = await sender.Send(new Command(userId, request.Username, request.FavouriteTeamId), ct);

            return result.ToApiResult();
        }
    }
}
