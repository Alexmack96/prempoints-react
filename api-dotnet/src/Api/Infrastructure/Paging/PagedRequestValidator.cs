using Api.Domain.Contracts;
using FluentValidation;

namespace Api.Infrastructure.Paging;

/// <summary>
/// The paging and sorting rules, written once.
/// <para>
/// A collection slice inherits this and adds only the rules specific to its own
/// filters. Previously each slice repeated these three, which is three chances
/// per resource to bound the page size differently or forget to bound it at all.
/// </para>
/// </summary>
public abstract class PagedRequestValidator<TRequest, TEntity> : AbstractValidator<TRequest>
    where TRequest : IPagedRequest
    where TEntity : IAuditableEntity
{
    protected PagedRequestValidator(SortMap<TEntity> sortMap)
    {
        ArgumentNullException.ThrowIfNull(sortMap);

        RuleFor(request => request.Page)
            .GreaterThanOrEqualTo(1)
            .When(request => request.Page.HasValue);

        // Refused rather than clamped: a caller that asks for 500 and silently
        // receives 100 pages through the collection wrongly and never finds out.
        RuleFor(request => request.PageSize)
            .InclusiveBetween(1, PagingDefaults.MaxPageSize)
            .When(request => request.PageSize.HasValue);

        RuleFor(request => request.Sort)
            .Must(sortMap.IsValid)
            .WithMessage($"Sort must be one of: {sortMap.Allowed}.");
    }
}
