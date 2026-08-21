using Api.Domain.Authorization;
using Api.Domain.Entities;
using Api.Infrastructure.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace IntegrationTests;

public class TestDataSeeder
{
    private readonly PremPointsDbContext _context;

    // We keep these explicitly typed so we can access their IDs later
    private SeasonEntity? _season2025;

    // Lists to hold data
    private readonly List<UserEntity> _users = new();
    private readonly List<UserSeasonEntity> _userSeasons = new();
    private readonly List<TeamEntity> _teams = new();
    private readonly List<TeamSeasonEntity> _teamSeasons = new();
    private readonly List<SeasonEntity> _seasons = new();
    private readonly List<SeasonPeriodEntity> _seasonPeriods = new();

    public TestDataSeeder(PremPointsDbContext context)
    {
        _context = context;
    }

    public async Task SeedAsync()
    {
        // 1. Prepare all data in memory with explicit IDs first
        PrepareSeasons();
        PrepareSeasonPeriods(); // dependent on Season ID
        PrepareTeams();
        PrepareUsers();

        // 2. Prepare Link Tables (Dependent on the IDs generated above)
        PrepareUserSeasons();
        PrepareTeamSeasons();

        // 3. Add to context
        await _context.Seasons.AddRangeAsync(_seasons);
        await _context.SeasonPeriods.AddRangeAsync(_seasonPeriods);
        await _context.Teams.AddRangeAsync(_teams);
        await _context.Users.AddRangeAsync(_users);
        await _context.UserSeasons.AddRangeAsync(_userSeasons);
        await _context.TeamSeasons.AddRangeAsync(_teamSeasons);

        // 4. Save once. 
        // Since IDs are pre-generated, EF creates optimized batch inserts easily.
        await _context.SaveChangesAsync();
    }

    private void PrepareSeasons()
    {
        // Generate explicit ID for 2025 so we can reference it immediately
        _season2025 = new SeasonEntity
        {
            Id = TestIds.Season(2025),
            SeasonName = "PremPoints 2025/26",
            StartYear = 2025
        };

        _seasons.Add(_season2025);

        _seasons.Add(new SeasonEntity
        {
            Id = TestIds.Season(2026),
            SeasonName = "PremPoints 2026/27",
            StartYear = 2026
        });
    }

    private void PrepareSeasonPeriods()
    {
        if (_season2025 is null)
            throw new Exception("Season 2025 must be initialized before periods.");

        // Helper local function to make the list cleaner
        SeasonPeriodEntity CreatePeriod(int gw, int startMonth, int d1, int endMonth, int d2, int year)
        {
            // Explicitly set ID and link the navigation property
            return new SeasonPeriodEntity
            {
                Id = TestIds.SeasonPeriod(gw),
                GameweekNumber = gw,
                PeriodStartDate = new DateOnly(year, startMonth, d1),
                PeriodEndDate = new DateOnly(year, endMonth, d2),
                Season = _season2025,
                SeasonId = _season2025.Id // Good practice to set both if you have them
            };
        }

        _seasonPeriods.AddRange([
            // AUGUST
            CreatePeriod(1, 8, 15, 8, 21, 2025),
            CreatePeriod(2, 8, 22, 8, 28, 2025),
            CreatePeriod(3, 8, 29, 9, 11, 2025),
            // SEPTEMBER
            CreatePeriod(4, 9, 12, 9, 18, 2025),
            CreatePeriod(5, 9, 19, 9, 25, 2025),
            CreatePeriod(6, 9, 26, 10, 2, 2025),
            CreatePeriod(7, 10, 3, 10, 16, 2025),
            // OCTOBER
            CreatePeriod(8, 10, 17, 10, 23,2025),
            CreatePeriod(9, 10, 24, 10, 30,2025),
            CreatePeriod(10, 10, 31, 11, 6,2025),
            CreatePeriod(11, 11, 7, 11, 20,2025),
            // NOVEMBER
            CreatePeriod(12, 11, 21, 11, 27,2025),
            CreatePeriod(13, 11, 28, 12, 4,2025),
            // DECEMBER
            CreatePeriod(14, 12, 5, 12, 11,2025),
            CreatePeriod(15, 12, 12, 12, 18,2025),
            new SeasonPeriodEntity { Id = TestIds.SeasonPeriod(16), GameweekNumber = 16, PeriodStartDate = new DateOnly(2025, 12, 19), PeriodEndDate = new DateOnly(2026, 1, 1), Season = _season2025 },
            // JANUARY (2026)
            CreatePeriod(17, 1, 2, 1, 8, 2026),
            CreatePeriod(18, 1, 9, 1, 15, 2026),
            CreatePeriod(19, 1, 16, 1, 22, 2026),
            CreatePeriod(20, 1, 23, 1, 29, 2026),
            CreatePeriod(21, 1, 30, 2, 5, 2026),
            // FEBRUARY
            CreatePeriod(22, 2, 6, 2, 12, 2026),
            CreatePeriod(23, 2, 13, 2, 19, 2026),
            CreatePeriod(24, 2, 20, 2, 26, 2026),
            CreatePeriod(25, 2, 27, 3, 12, 2026),
            // MARCH
            CreatePeriod(26, 3, 13, 3, 19, 2026),
            CreatePeriod(27, 3, 20, 4, 9, 2026),
            // APRIL
            CreatePeriod(28, 4, 10, 4, 16, 2026),
            CreatePeriod(29, 4, 17, 4, 23, 2026),
            CreatePeriod(30, 4, 24, 4, 30, 2026),
            // MAY
            CreatePeriod(31, 5, 1, 5, 7, 2026),
            CreatePeriod(32, 5, 8, 5, 14, 2026),
            CreatePeriod(33, 5, 15, 5, 21, 2026),
            CreatePeriod(34, 5, 22, 8, 1, 2026)
        ]);
    }

    private void PrepareTeams()
    {
        var teamNames = new[]
        {
            "Arsenal", "Aston Villa", "Brentford", "Brighton", "Bournemouth",
            "Chelsea", "Crystal Palace", "Everton", "Fulham", "Liverpool",
            "Manchester City", "Manchester United", "Newcastle", "Nottingham Forest",
            "Tottenham", "West Ham", "Wolves", "Burnley", "Leeds United", "Sunderland"
        };

        foreach (var (name, index) in teamNames.Select((name, index) => (name, index)))
        {
            _teams.Add(new TeamEntity
            {
                Id = TestIds.Team(index + 1),
                TeamName = name
            });
        }
    }

    private void PrepareUsers()
    {
        _users.AddRange(new[]
        {
            new UserEntity { Id = TestIds.User(1), WorkOSUserId = "user_1", Username = "Almack", FirstName = "Alex", LastName = "Mackintosh", Email = "am@gmail.com", Role = UserRole.Administrator },
            new UserEntity { Id = TestIds.User(2), WorkOSUserId = "user_2", Username = "KC", FirstName = "Casey", LastName = "Liddy", Email = "cl@gmail.com", Role = UserRole.Standard },
            new UserEntity { Id = TestIds.User(3), WorkOSUserId = "user_3", Username = "Andy", FirstName = "Andrew", LastName = "Mackintosh", Email = "ajmack@gmail.com", Role = UserRole.Standard }
        });
    }

    private void PrepareUserSeasons()
    {
        if (_season2025 is null) throw new Exception("Season must exist.");

        foreach (var user in _users)
        {
            _userSeasons.Add(new UserSeasonEntity
            {
                Id = TestIds.UserSeason(_userSeasons.Count + 1),
                Season = _season2025,
                SeasonId = _season2025.Id,
                User = user,
                UserId = user.Id
            });
        }
    }

    private void PrepareTeamSeasons()
    {
        if (_season2025 is null) throw new Exception("Season must exist.");

        foreach (var team in _teams)
        {
            _teamSeasons.Add(new TeamSeasonEntity
            {
                Id = TestIds.TeamSeason(_teamSeasons.Count + 1),
                Season = _season2025,
                SeasonId = _season2025.Id,
                Team = team,
                TeamId = team.Id
            });
        }
    }

    public async Task<TeamEntity> GetTeamAsync(PremPointsDbContext activeContext, string teamName)
    {
        return await activeContext.Teams.SingleAsync(te => te.TeamName == teamName);
    }

    public async Task<UserEntity> GetUserAsync(PremPointsDbContext activeContext, string username)
    {
        return await activeContext.Users.SingleAsync(te => te.Username == username);
    }

    public async Task<SeasonEntity> GetSeasonAsync(PremPointsDbContext activeContext, int startYear = 2025)
    {
        return await activeContext.Seasons.SingleAsync(s => s.StartYear == startYear);
    }

    public async Task<SeasonPeriodEntity> GetSeasonPeriodAsync(PremPointsDbContext activeContext, int gameweekNumber, int startYear)
    {
        return await activeContext.SeasonPeriods
            .Include(sp => sp.Season)
            .SingleAsync(s => s.GameweekNumber == gameweekNumber && s.Season.StartYear == startYear);
    }

    public async Task<TeamSeasonEntity> GetTeamSeasonAsync(PremPointsDbContext activeContext, string teamName, int seasonStartYear)
    {
        return await activeContext.TeamSeasons
            .Include(ts => ts.Team)
            .Include(ts => ts.Season)
            .SingleAsync(ts => ts.Team.TeamName == teamName && ts.Season.StartYear == seasonStartYear);
    }
}