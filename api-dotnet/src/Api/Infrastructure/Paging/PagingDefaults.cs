namespace Api.Infrastructure.Paging;

/// <summary>
/// The paging bounds every collection endpoint validates against.
/// <para>
/// <see cref="MaxPageSize"/> is a cap, not a suggestion: without it a caller
/// can ask for <c>pageSize=1000000</c> and turn a paged endpoint back into an
/// unbounded table scan.
/// </para>
/// </summary>
public static class PagingDefaults
{
    public const int DefaultPage = 1;
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    public static int NormalisePage(int? page) => page is null or < DefaultPage ? DefaultPage : page.Value;

    public static int NormalisePageSize(int? pageSize) => pageSize switch
    {
        null or < 1 => DefaultPageSize,
        > MaxPageSize => MaxPageSize,
        _ => pageSize.Value,
    };
}
