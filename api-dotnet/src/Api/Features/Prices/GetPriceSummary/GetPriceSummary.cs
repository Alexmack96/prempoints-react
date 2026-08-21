using Api.Infrastructure.Endpoints;
using Api.Infrastructure.Paging;
using Ardalis.Result;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Api.Features.Prices.GetPriceSummary;

/// <summary>
/// The price board: every club with its latest quote and which way that quote
/// last moved.
/// <para>
/// A read model rather than a plain collection of prices. The page needs teams
/// joined to their most recent price and the one before it, which is three
/// round trips and a grouping if a client assembles it from /teams and /prices
/// — and every client would have to get the same join right.
/// </para>
/// </summary>
public static class GetPriceSummary
{
    public record Query(DateOnly? AsAtDate, int Page, int PageSize)
        : IRequest<Result<PagedResponse<TeamPriceSummaryDto>>>;

    public record Request(DateOnly? AsAtDate, int? Page, int? PageSize) : IPagedRequest
    {
        /// Sorting is fixed: the board reads highest price first, like every
        /// other outright market anyone using this will have seen.
        public string? Sort => null;
    }

    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
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

            app.MapGet("prices/summary", HandleAsync)
               .WithName("GetPriceSummary")
               .WithTags("Prices")
               .WithSummary("Every club with its latest price and how that price moved.")
               .WithValidation<Request>()
               .Produces<PagedResponse<TeamPriceSummaryDto>>(StatusCodes.Status200OK);
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
