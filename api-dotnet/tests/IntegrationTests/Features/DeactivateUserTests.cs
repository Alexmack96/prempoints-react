using Api.Domain.Authorization;
using Api.Domain.Entities;
using Api.Features.Trades;
using Api.Features.UserSeasons.DeactivateUser;




using Api.Infrastructure.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace IntegrationTests.Features;

public class DeactivateUserTests : BaseIntegrationTest
{
    private const string BaseUrl = "api/v1/users/deactivate";

    [Fact]
    public async Task DeactivateUser_ReturnsOk_AndActivatesUser()
    {
        var ct = TestContext.Current.CancellationToken;
        var asAtDate = new DateOnly(2025, 08, 15);

        // Arrage

        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PremPointsDbContext>();

        var season = await DataSeeder.GetSeasonAsync(context, 2025);
        var seasonPeriod = await DataSeeder.GetSeasonPeriodAsync(context, 1, 2025);
        var team = await DataSeeder.GetTeamAsync(context, "Chelsea");

        var newPrice = new PriceEntity { Id = Guid.CreateVersion7(), Bid = 39.5m, Ask = 40.5m, ValueDate = asAtDate, Team = team, SeasonPeriod = seasonPeriod };
        context.Prices.Add(newPrice);

        var newUser = new UserEntity { Id = Guid.CreateVersion7(), WorkOSUserId = "user_1", Username = "TEST", FirstName = "TEST", LastName = "TEST", Role = UserRole.Standard };
        context.Users.Add(newUser);
        await context.SaveChangesAsync(ct);
        var tradeDate = new DateTime(2025, 8, 15, 12, 0, 0, DateTimeKind.Utc);
        var tradeEntity = new TradeEntity
        {
            Id = Guid.CreateVersion7(),
            Exposure = 40,
            TimezoneIana = "Europe/London",
            TradeDateUtc = tradeDate,
            Team = team,
            SeasonPeriod = seasonPeriod,
            User = newUser,
            Price = newPrice,
            TradeType = TradeType.Standard,
        };

        context.Trades.Add(tradeEntity);

        var userSeason = new UserSeasonEntity { Id = Guid.CreateVersion7(), User = newUser, Season = season };
        context.UserSeasons.Add(userSeason);
        await context.SaveChangesAsync(ct);

        // Check if the user exists in the database
        Assert.NotNull(newUser);
        Assert.Equal("TEST", newUser.Username);
        Assert.Equal(UserRole.Standard, newUser.Role);

        var username = "TEST";
        var request = new DeactivateUser.Request(asAtDate);

        Dictionary<string, string> parameters = [];

        parameters["asAtDate"] = asAtDate.ToString("O");
        var endpoint = $"{BaseUrl}/{username}";

        var uri = endpoint.AddQueryString(parameters);

        //Act
        var response = await AsAdmin().PostAsJsonAsync(uri, request, ct);

        //Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
