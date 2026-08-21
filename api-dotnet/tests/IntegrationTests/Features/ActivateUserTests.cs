using Api.Domain.Authorization;
using Api.Domain.Entities;
using Api.Features.UserSeasons;
using Api.Features.UserSeasons.ActivateUser;
using Api.Infrastructure.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace IntegrationTests.Features;

public class ActivateUserTests : BaseIntegrationTest
{

    [Fact]
    public async Task ActivateUser_ReturnsOk_AndActivatesUser()
    {
        var ct = TestContext.Current.CancellationToken;
        var baseUrl = "api/v1/users/activate";

        // Arrage
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PremPointsDbContext>();

        var newUser = new UserEntity { Id = Guid.CreateVersion7(), WorkOSUserId = "user_1", Username = "TEST", FirstName = "TEST", LastName = "TEST", Email = "TEST@TEST.COM", Role = UserRole.Standard };
        context.Users.Add(newUser);
        await context.SaveChangesAsync(ct);

        // Check if the user exists in the database
        Assert.NotNull(newUser);
        Assert.Equal("TEST", newUser.Username);
        Assert.Equal(UserRole.Standard, newUser.Role);

        var expectedSeason = await DataSeeder.GetSeasonAsync(context, 2025);

        var username = "TEST";
        var asAtDate = new DateOnly(2025, 08, 15);
        var expectedLateJoinerFee = 10;
        var request = new ActivateUser.Request(asAtDate, expectedLateJoinerFee);

        Dictionary<string, string> parameters = [];

        parameters["asAtDate"] = asAtDate.ToString("O");
        parameters["lateJoinerFee"] = expectedLateJoinerFee.ToString();
        var endpoint = $"{baseUrl}/{username}";

        var uri = endpoint.AddQueryString(parameters);

        //Act
        var response = await HttpClient.PostAsJsonAsync(uri, request, ct);

        //Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await DeserializeAsync<UserSeasonDto>(response, ct);

        Assert.NotNull(result);
        Assert.Equal(expectedLateJoinerFee, result.LateJoinerFee);
        Assert.Equal(expectedSeason.Id, result.SeasonId);
        Assert.Equal(newUser.Id, result.UserId);
    }
}
