namespace IntegrationTests;

/// <summary>
/// Stable, readable primary keys for seeded data.
/// <para>
/// The seeder used <c>Guid.CreateVersion7()</c>, which is random in its low
/// bits. That is invisible until something ties: every seeded row is written in
/// one <c>SaveChanges</c> against a pinned clock, so they all share a
/// <c>CreatedAtUtc</c>, and a sort on that column falls through to the id as its
/// tiebreaker. With random ids the resulting order — and therefore any snapshot
/// of it — changes between runs.
/// </para>
/// <para>
/// The shape <c>0000000K-0000-0000-0000-00000000000N</c> keeps the kind and the
/// index legible when one does surface in a failure message.
/// </para>
/// </summary>
internal static class TestIds
{
    private const int SeasonKind = 1;
    private const int SeasonPeriodKind = 2;
    private const int TeamKind = 3;
    private const int UserKind = 4;
    private const int UserSeasonKind = 5;
    private const int TeamSeasonKind = 6;

    public static Guid Season(int index) => Make(SeasonKind, index);
    public static Guid SeasonPeriod(int index) => Make(SeasonPeriodKind, index);
    public static Guid Team(int index) => Make(TeamKind, index);
    public static Guid User(int index) => Make(UserKind, index);
    public static Guid UserSeason(int index) => Make(UserSeasonKind, index);
    public static Guid TeamSeason(int index) => Make(TeamSeasonKind, index);

    private static Guid Make(int kind, int index) =>
        Guid.Parse($"{kind:D8}-0000-0000-0000-{index:D12}");
}
