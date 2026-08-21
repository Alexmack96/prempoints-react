using Api.Domain.Entities;
using Api.Infrastructure.EntityFramework;
using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static Api.Features.Admin.SeedNewSeason.SeedNewSeason;

namespace Api.Features.Admin.SeedNewSeason;

/// <summary>
/// Stands up a whole season: the season row, its gameweeks, any clubs that are
/// new to the league, and the twenty enrolments that say who is playing.
/// <para>
/// This previously created the season row alone and silently ignored the
/// promoted and relegated lists it was given, which made the endpoint's name a
/// lie and left no way to populate teams at all.
/// </para>
/// <para>
/// The roster carries forward: last season's clubs, minus the relegated, plus
/// the promoted. For the first ever season there is nothing to carry, so the
/// promoted list is taken as the full roster.
/// </para>
/// </summary>
public class SeedNewSeasonHandler(
    ILogger<SeedNewSeasonHandler> logger,
    PremPointsDbContext context,
    TimeProvider clock)
    : IRequestHandler<Command, Result<SeedNewSeasonResult>>
{
    private const int GameweekLengthInDays = 7;

    public async Task<Result<SeedNewSeasonResult>> Handle(Command command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.EndDate <= command.StartDate)
        {
            return Result.Invalid(new ValidationError
            {
                Identifier = nameof(command.EndDate),
                ErrorMessage = "The season must end after it starts.",
            });
        }

        var startYear = command.StartDate.Year;

        // StartYear carries a unique index, so a second seed of the same season
        // would fail deep in SaveChanges. Asked here instead, so re-running the
        // endpoint gives a clear answer rather than a constraint violation.
        if (await context.Seasons.AnyAsync(s => s.StartYear == startYear, cancellationToken))
        {
            return Result.Conflict($"A season starting in {startYear} already exists.");
        }

        var roster = await BuildRosterAsync(command, startYear, cancellationToken);
        if (!roster.IsSuccess)
        {
            return Result.Conflict([.. roster.Errors]);
        }

        var teamNames = roster.Value;

        var season = new SeasonEntity
        {
            // Auditable ids are ValueGenerated.Never, so nothing downstream
            // fills these in — an unset id collides on the second insert.
            Id = Guid.CreateVersion7(clock.GetUtcNow()),
            SeasonName = command.SeasonName,
            StartYear = startYear,
        };
        context.Seasons.Add(season);

        var gameweeks = BuildGameweeks(command, season);
        context.SeasonPeriods.AddRange(gameweeks);

        var (teams, created) = await ResolveTeamsAsync(teamNames, cancellationToken);

        foreach (var team in teams)
        {
            context.TeamSeasons.Add(new TeamSeasonEntity
            {
                Id = Guid.CreateVersion7(clock.GetUtcNow()),
                Team = team,
                Season = season,
            });
        }

        try
        {
            // One SaveChanges: a season with no gameweeks, or gameweeks with no
            // enrolments, is worse than no season at all, and the caller would
            // have no way to tell how far it got.
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            logger.LogError(exception, "Seeding season {SeasonName} failed.", command.SeasonName);

            // Gameweek start and end dates are globally unique, not unique per
            // season, so overlapping an existing season's dates lands here.
            return Result.Conflict(
                "Could not seed the season. The most likely cause is gameweek dates overlapping an " +
                "existing season, since period start and end dates are unique across the whole table.");
        }

        logger.LogInformation(
            "Seeded season {SeasonName}: {Gameweeks} gameweeks, {Enrolled} teams, {Created} newly created.",
            season.SeasonName,
            gameweeks.Count,
            teams.Count,
            created.Count);

        return new SeedNewSeasonResult
        {
            SeasonId = season.Id,
            SeasonName = season.SeasonName,
            StartYear = season.StartYear,
            GameweeksCreated = gameweeks.Count,
            TeamsCreated = created,
            TeamsEnrolled = [.. teams.Select(team => team.TeamName).OrderBy(name => name, StringComparer.Ordinal)],
        };
    }

    /// <summary>
    /// Last season's clubs, minus the relegated, plus the promoted.
    /// </summary>
    private async Task<Result<List<string>>> BuildRosterAsync(
        Command command,
        int startYear,
        CancellationToken cancellationToken)
    {
        var promoted = Clean(command.PromotedTeams);
        var relegated = Clean(command.RelegatedTeams);

        var previousSeason = await context.Seasons
            .Where(s => s.StartYear < startYear)
            .OrderByDescending(s => s.StartYear)
            .FirstOrDefaultAsync(cancellationToken);

        if (previousSeason is null)
        {
            // Nothing to carry forward, so the promoted list is the league.
            // This is the path an empty database takes.
            if (promoted.Count == 0)
            {
                return Result.Conflict(
                    "There is no earlier season to carry teams forward from, so PromotedTeams must " +
                    "contain the full roster for this season.");
            }

            return promoted;
        }

        var previousRoster = await context.TeamSeasons
            .Where(ts => ts.SeasonId == previousSeason.Id)
            .Select(ts => ts.Team.TeamName)
            .ToListAsync(cancellationToken);

        // Naming a club that was not in last season's league is almost always a
        // typo, and silently ignoring it leaves a 21-team season.
        var notInPreviousSeason = relegated
            .Where(name => !previousRoster.Contains(name, StringComparer.OrdinalIgnoreCase))
            .ToList();

        if (notInPreviousSeason.Count > 0)
        {
            return Result.Conflict(
                $"These clubs cannot be relegated because they were not in the {previousSeason.SeasonName} " +
                $"season: {string.Join(", ", notInPreviousSeason)}.");
        }

        return previousRoster
            .Where(name => !relegated.Contains(name, StringComparer.OrdinalIgnoreCase))
            .Concat(promoted)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Weekly gameweeks across the season, with the last one truncated to the
    /// end date rather than running past it.
    /// </summary>
    private List<SeasonPeriodEntity> BuildGameweeks(Command command, SeasonEntity season)
    {
        var gameweeks = new List<SeasonPeriodEntity>();
        var number = 1;

        for (var start = command.StartDate; start <= command.EndDate; start = start.AddDays(GameweekLengthInDays))
        {
            var end = start.AddDays(GameweekLengthInDays - 1);
            if (end > command.EndDate)
            {
                end = command.EndDate;
            }

            gameweeks.Add(new SeasonPeriodEntity
            {
                Id = Guid.CreateVersion7(clock.GetUtcNow()),
                GameweekNumber = number++,
                PeriodStartDate = start,
                PeriodEndDate = end,
                Season = season,
            });
        }

        return gameweeks;
    }

    /// <summary>
    /// Existing clubs are reused, not duplicated — TeamName is unique, and a
    /// promoted club returning after a season away is the normal case.
    /// </summary>
    private async Task<(List<TeamEntity> Teams, List<string> Created)> ResolveTeamsAsync(
        List<string> teamNames,
        CancellationToken cancellationToken)
    {
        var existing = await context.Teams
            .Where(team => teamNames.Contains(team.TeamName))
            .ToListAsync(cancellationToken);

        var byName = existing.ToDictionary(team => team.TeamName, StringComparer.OrdinalIgnoreCase);

        var teams = new List<TeamEntity>();
        var created = new List<string>();

        foreach (var name in teamNames)
        {
            if (byName.TryGetValue(name, out var team))
            {
                teams.Add(team);
                continue;
            }

            var newTeam = new TeamEntity
            {
                Id = Guid.CreateVersion7(clock.GetUtcNow()),
                TeamName = name,
            };

            context.Teams.Add(newTeam);
            byName[name] = newTeam;
            teams.Add(newTeam);
            created.Add(name);
        }

        return (teams, created);
    }

    private static List<string> Clean(IEnumerable<string>? names) =>
    [
        .. (names ?? [])
            .Select(name => name?.Trim() ?? string.Empty)
            .Where(name => name.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase),
    ];
}
