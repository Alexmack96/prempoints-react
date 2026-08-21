using Api.Infrastructure.Endpoints;
using Api.Infrastructure.Endpoints.Filters;
using Api.Infrastructure.Paging;
using Ardalis.Result;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Api.Features.Leaderboard.GetLeaderboard;

/// <summary>
/// The season standings: every enrolled player and what they are up or down.
/// <para>
/// A read model rather than something a client assembles. The alternative is
/// fetching the active users, then every trade, then every price, and getting
/// the same join right in each client — and the standings are the one screen
/// where two clients disagreeing would be obvious.
/// </para>
/// </summary>
public static class GetLeaderboard
{
    public record Query(DateOnly? AsAtDate, int Page, int PageSize)
        : IRequest<Result<PagedResponse<LeaderboardRowDto>>>;

    public record Request(DateOnly? AsAtDate, int? Page, int? PageSize) : IPagedRequest
    {
        /// Sorting is fixed: a leaderboard reads best-first, and a client that
        /// could reorder it would be showing something other than the standings.
        public string? Sort => null;
    }

    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(request => request.AsAtDate)
                .GreaterThanOrEqualTo(new DateOnly(2025, 1, 1))
                .When(request => request.AsAtDate.HasValue);

            RuleFor(request => request.Page).GreaterThanOrEqualTo(1).When(r => r.Page.HasValue);
            RuleFor(request => request.PageSize)
                .InclusiveBetween(1, PagingDefaults.MaxPageSize)
                .When(r => r.PageSize.HasValue);
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            ArgumentNullException.ThrowIfNull(app);

            app.MapGet("leaderboard", HandleAsync)
               .WithName("GetLeaderboard")
               .WithTags("Leaderboard")
               .WithSummary("Every player enrolled in the season, best first.")
               .WithValidation<Request>()
               .Produces<PagedResponse<LeaderboardRowDto>>(StatusCodes.Status200OK)
               .ProducesProblem(StatusCodes.Status404NotFound);
        }

        public static async Task<IResult> HandleAsync(
            [AsParameters] Request request,
            [FromServices] ISender sender,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(sender);

            var query = new Query(
                request.AsAtDate,
                PagingDefaults.NormalisePage(request.Page),
                PagingDefaults.NormalisePageSize(request.PageSize));

            var result = await sender.Send(query, ct);

            return result.ToApiResult();
        }
    }
}
