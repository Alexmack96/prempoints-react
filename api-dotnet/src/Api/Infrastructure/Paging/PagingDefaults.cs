namespace Api.Infrastructure.Paging;

/// <summary>
/// The paging defaults. Filling in absent values only — deciding what happens
/// when a value is out of range is the validator's job.
/// <para>
/// This used to clamp a too-large page size down to the maximum as well, which
/// contradicted the validator that refuses it. Two answers to one question is
/// worse than either answer: a slice that forgot the validator would silently
/// clamp, and its caller would page through the collection wrongly while
/// believing it had asked for more.
/// </para>
/// </summary>
public static class PagingDefaults
{
    public const int DefaultPage = 1;
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    public static int NormalisePage(int? page) => page ?? DefaultPage;

    public static int NormalisePageSize(int? pageSize) => pageSize ?? DefaultPageSize;
}
