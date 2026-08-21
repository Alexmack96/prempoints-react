namespace Api.Infrastructure.Paging;

/// <summary>
/// The paging and sorting parameters every collection endpoint accepts.
/// <para>
/// An interface rather than a base record because slice requests are bound with
/// <c>[AsParameters]</c>, which needs a concrete type with a constructor it can
/// see. Implementing this gets the slice a shared validator
/// (<see cref="PagedRequestValidator{T}"/>) instead of a fourth copy of the
/// same three rules.
/// </para>
/// </summary>
public interface IPagedRequest
{
    int? Page { get; }
    int? PageSize { get; }
    string? Sort { get; }
}
