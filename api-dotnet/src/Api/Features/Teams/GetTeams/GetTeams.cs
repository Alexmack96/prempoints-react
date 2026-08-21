using Api.Domain.Entities;
using Api.Infrastructure.Endpoints;
using Api.Infrastructure.Endpoints.Filters;
using Api.Infrastructure.Paging;
using Ardalis.Result;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Api.Features.Teams.GetTeams;

/// <summary>
/// The one read-collection for teams. "Active" and "by name" are filters here
/// rather than routes of their own, so there is a single paging, sorting and
/// filtering contract for a client to learn and a single shape for the other
/// resources to copy.
/// </summary>
public static class GetTeams
{
    public record Query(DateOnly? ActiveOn, string? Name, string? Sort, int Page, int PageSize)
        : IRequest<Result<PagedResponse<TeamDto>>>;

    public record Request(DateOnly? ActiveOn, string? Name, string? Sort, int? Page, int? PageSize)
        : IPagedRequest;

    /// Paging and sort rules come from the base; only the filters below are
    /// this slice's own.
    public class Validator : PagedRequestValidator<Request, TeamEntity>
    {
        public Validator() : base(TeamSort.Map)
        {
            RuleFor(request => request.ActiveOn)
                .GreaterThanOrEqualTo(new DateOnly(2025, 1, 1))
                .When(request => request.ActiveOn.HasValue);

            RuleFor(request => request.Name)
                .MaximumLength(50)
                .When(request => request.Name is not null);
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            ArgumentNullException.ThrowIfNull(app);

            app.MapGet("teams", HandleAsync)
               .WithName("GetTeams")
               .WithTags("Teams")
               .WithSummary("List teams, optionally filtered by season activity and name.")
               .WithValidation<Request>()
               .Produces<PagedResponse<TeamDto>>(StatusCodes.Status200OK);
        }

        public static async Task<IResult> HandleAsync(
            [AsParameters] Request request,
            [FromServices] ISender sender,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(sender);
            ArgumentNullException.ThrowIfNull(request);

            var query = new Query(
                request.ActiveOn,
                request.Name,
                request.Sort,
                PagingDefaults.NormalisePage(request.Page),
                PagingDefaults.NormalisePageSize(request.PageSize));

            var result = await sender.Send(query, ct);

            return result.ToApiResult();
        }
    }
}
