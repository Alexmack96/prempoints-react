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
/// The one read-collection for teams. "Active" and "by name" are filters on
/// this endpoint rather than routes of their own, so there is a single paging,
/// sorting and filtering contract for a client to learn and a single OpenAPI
/// shape for the other entities to copy.
/// </summary>
public static class GetTeams
{
    public record Query(DateOnly? ActiveOn, string? Name, string? Sort, int Page, int PageSize)
        : IRequest<Result<PagedResponse<TeamDto>>>;

    public record Request(DateOnly? ActiveOn, string? Name, string? Sort, int? Page, int? PageSize);

    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(r => r.ActiveOn)
                .GreaterThanOrEqualTo(new DateOnly(2025, 1, 1))
                .When(r => r.ActiveOn.HasValue);

            RuleFor(r => r.Page)
                .GreaterThanOrEqualTo(1)
                .When(r => r.Page.HasValue);

            // Bounded rather than clamped, so a caller asking for 10,000 rows is
            // told no instead of silently receiving 100 and mispaging.
            RuleFor(r => r.PageSize)
                .InclusiveBetween(1, PagingDefaults.MaxPageSize)
                .When(r => r.PageSize.HasValue);

            RuleFor(r => r.Name)
                .MaximumLength(50)
                .When(r => r.Name is not null);

            RuleFor(r => r.Sort)
                .Must(TeamSort.IsValid)
                .When(r => !string.IsNullOrWhiteSpace(r.Sort))
                .WithMessage($"Sort must be one of: {TeamSort.Allowed}.");
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
               .RequireRateLimiting("DefaultPolicy")
               .AddEndpointFilter<ValidationFilter<Request>>()
               .Produces<PagedResponse<TeamDto>>(StatusCodes.Status200OK)
               .ProducesValidationProblem();
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
