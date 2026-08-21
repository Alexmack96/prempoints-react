namespace Api.Infrastructure.Paging;

/// <summary>
/// The response envelope every collection endpoint returns.
/// <para>
/// One shape for every list in the API, so a client that can page one resource
/// can page all of them, and so the OpenAPI document describes paging once.
/// Offset paging rather than keyset: the admin surface wants a total and the
/// ability to jump to a page, and no collection here is large enough for the
/// <c>COUNT(*)</c> to matter.
/// </para>
/// </summary>
public sealed record PagedResponse<T>
{
    public required IReadOnlyList<T> Items { get; init; }
    public required int Page { get; init; }
    public required int PageSize { get; init; }
    public required int TotalCount { get; init; }

    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public static PagedResponse<T> Create(IReadOnlyList<T> items, int page, int pageSize, int totalCount) =>
        new() { Items = items, Page = page, PageSize = pageSize, TotalCount = totalCount };

    /// <summary>
    /// An empty page. A filtered collection that matches nothing is a 200 with
    /// no items — never a 404, and never an error.
    /// </summary>
    public static PagedResponse<T> Empty(int page, int pageSize) =>
        new() { Items = [], Page = page, PageSize = pageSize, TotalCount = 0 };
}
