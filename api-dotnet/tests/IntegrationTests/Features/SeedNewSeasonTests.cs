using Api.Features.Admin.SeedNewSeason;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;

namespace IntegrationTests.Features;

/// <summary>
/// POST /api/v1/seednewseason — how a new season gets stood up, including in
/// production. The seeded database already holds a 2025/26 season with twenty
/// enrolled clubs and an empty 2026/27 season.
/// </summary>
public class SeedNewSeasonTests : BaseIntegrationTest
{
    private const string Url = "/api/v1/seednewseason";

    /// Dates well clear of the seeded gameweeks. Period start and end dates are
    /// unique across the whole table, not per season, so an overlap is a
    /// conflict rather than a merge.
    private static readonly DateOnly SeasonStart = new(2027, 8, 7);
    private static readonly DateOnly SeasonEnd = new(2028, 5, 20);

    private static readonly string[] FullRoster =
    [
        "Arsenal", "Aston Villa", "Bournemouth", "Brentford", "Brighton",
        "Chelsea", "Coventry", "Crystal Palace", "Everton", "Fulham",
        "Hull", "Ipswich", "Leeds United", "Liverpool", "Manchester City",
        "Manchester United", "Newcastle", "Nottingham Forest", "Sunderland", "Tottenham",
    ];

    private static SeedNewSeason.Request Request(
        string seasonName = "PremPoints 2027/28",
        IEnumerable<string>? promoted = null,
        IEnumerable<string>? relegated = null) =>
        new(seasonName, SeasonStart, SeasonEnd, [.. promoted ?? FullRoster], [.. relegated ?? []]);

    [Fact]
    public async Task CreatesSeasonGameweeksAndEnrolments()
    {
        var response = await AsAdmin().PostAsJsonAsync(Url, Request(), TestContext.Current.CancellationToken);

        await VerifyResponse(response);
    }

    [Fact]
    public async Task EnrolsEveryTeamInTheNewSeason()
    {
        await AsAdmin().PostAsJsonAsync(Url, Request(), TestContext.Current.CancellationToken);

        var enrolled = await WithDbAsync(db => db.TeamSeasons
            .Where(ts => ts.Season.StartYear == 2027)
            .CountAsync());

        Assert.Equal(FullRoster.Length, enrolled);
    }

    [Fact]
    public async Task CreatesWeeklyGameweeksCoveringTheWholeSeason()
    {
        await AsAdmin().PostAsJsonAsync(Url, Request(), TestContext.Current.CancellationToken);

        var gameweeks = await WithDbAsync(db => db.SeasonPeriods
            .Where(sp => sp.Season.StartYear == 2027)
            .OrderBy(sp => sp.GameweekNumber)
            .ToListAsync());

        Assert.Equal(SeasonStart, gameweeks[0].PeriodStartDate);
        // The last gameweek is truncated to the season end rather than running
        // past it, so the season covers exactly the range asked for.
        Assert.Equal(SeasonEnd, gameweeks[^1].PeriodEndDate);
        Assert.Equal(gameweeks.Count, gameweeks[^1].GameweekNumber);
    }

    [Fact]
    public async Task ReusesExistingTeamsRatherThanDuplicatingThem()
    {
        await AsAdmin().PostAsJsonAsync(Url, Request(), TestContext.Current.CancellationToken);

        // Arsenal is already in the database from the 2025/26 season. TeamName
        // is unique, so a second row would be a constraint violation — this
        // guards the lookup that prevents one.
        var arsenals = await WithDbAsync(db => db.Teams.CountAsync(t => t.TeamName == "Arsenal"));

        Assert.Equal(1, arsenals);
    }

    [Fact]
    public async Task ReturnsConflict_WhenThatSeasonYearAlreadyExists()
    {
        var response = await AsAdmin().PostAsJsonAsync(
            Url,
            new SeedNewSeason.Request(
                "PremPoints 2025/26 again",
                new DateOnly(2025, 8, 15),
                new DateOnly(2026, 5, 20),
                [.. FullRoster],
                []),
            TestContext.Current.CancellationToken);

        await VerifyResponse(response);
    }

    [Fact]
    public async Task CarriesTheRosterForwardMinusRelegatedPlusPromoted()
    {
        // Drop the empty 2026/27 season so the new season's predecessor is
        // 2025/26, which has a full roster to carry forward.
        await WithDbAsync(async db =>
        {
            var empty = await db.Seasons.Where(s => s.StartYear == 2026).ToListAsync();
            db.Seasons.RemoveRange(empty);
            await db.SaveChangesAsync();
        });

        var response = await AsAdmin().PostAsJsonAsync(
            Url,
            Request(promoted: ["Wrexham"], relegated: ["Sunderland"]),
            TestContext.Current.CancellationToken);

        await VerifyResponse(response);
    }

    [Fact]
    public async Task ReturnsConflict_WhenRelegatingAClubThatWasNotInThePreviousSeason()
    {
        await WithDbAsync(async db =>
        {
            var empty = await db.Seasons.Where(s => s.StartYear == 2026).ToListAsync();
            db.Seasons.RemoveRange(empty);
            await db.SaveChangesAsync();
        });

        // Almost always a typo, and ignoring it silently leaves a 21-team league.
        var response = await AsAdmin().PostAsJsonAsync(
            Url,
            Request(promoted: ["Wrexham"], relegated: ["Sundrland"]),
            TestContext.Current.CancellationToken);

        await VerifyResponse(response);
    }

    [Fact]
    public async Task ReturnsUnprocessable_WhenTheSeasonEndsBeforeItStarts()
    {
        var response = await AsAdmin().PostAsJsonAsync(
            Url,
            new SeedNewSeason.Request(
                "Backwards",
                new DateOnly(2028, 5, 20),
                new DateOnly(2027, 8, 7),
                [.. FullRoster],
                []),
            TestContext.Current.CancellationToken);

        await VerifyResponse(response);
    }
}
