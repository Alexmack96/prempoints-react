namespace Api.Features.Admin.SeedNewSeason;

/// <summary>
/// What the seed actually did. A seeding endpoint that answers "Success" tells
/// you nothing you can check — this is the difference between believing the
/// season is set up and knowing it.
/// </summary>
public sealed record SeedNewSeasonResult
{
    public required Guid SeasonId { get; init; }
    public required string SeasonName { get; init; }
    public required int StartYear { get; init; }
    public required int GameweeksCreated { get; init; }

    /// Clubs that did not exist in the database before this call.
    public required IReadOnlyList<string> TeamsCreated { get; init; }

    /// The full roster enrolled in this season. Should be twenty.
    public required IReadOnlyList<string> TeamsEnrolled { get; init; }
}
