using Api.Domain.Entities;
using Api.Infrastructure.Endpoints;
using Api.Infrastructure.Paging;
using Ardalis.Result;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Api.Features.Prices.GetPrices;

/// <summary>
/// The price board. Reading prices had no endpoint at all until now, which
/// meant the trade board could not show players the number they were trading
/// against.
/// </summary>
public static class GetPrices
{
    public record Query(DateOnly? ValueDate, string? TeamName, string? Sort, int Page, int PageSize)
        : IRequest<Result<PagedResponse<PriceDto>>>;

    public record Request(DateOnly? ValueDate, string? TeamName, string? Sort, int? Page, int? PageSize)
        : IPagedRequest;

    public class Validator : PagedRequestValidator<Request, PriceEntity>
    {
        public Validator() : base(PriceSort.Map)
        {
            RuleFor(request => request.TeamName)
                .MaximumLength(50)
                .When(request => request.TeamName is not null);
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            ArgumentNullException.ThrowIfNull(app);

            app.MapGet("prices", HandleAsync)
               .WithName("GetPrices")
               .WithTags("Prices")
               .WithSummary("List prices, optionally for one value date or team.")
               .WithValidation<Request>()
               .Produces<PagedResponse<PriceDto>>(StatusCodes.Status200OK);
        }

        public static async Task<IResult> HandleAsync(
            [AsParameters] Request request,
            [FromServices] ISender sender,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(sender);

            var query = new Query(
                request.ValueDate,
                request.TeamName,
                request.Sort,
                PagingDefaults.NormalisePage(request.Page),
                PagingDefaults.NormalisePageSize(request.PageSize));

            var result = await sender.Send(query, ct);

            return result.ToApiResult();
        }
    }
}
