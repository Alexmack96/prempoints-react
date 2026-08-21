using Api.Domain.Authorization;
using Api.Features.Users;
using Api.Features.Users.CreateUser;
using Api.Infrastructure.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace IntegrationTests.Features;

public class CreateUserTests : BaseIntegrationTest
{

    [Fact]
    public async Task CreateUser_ReturnsOk_AndCreatesUser()
    {
        var ct = TestContext.Current.CancellationToken;
        var request = new CreateUser.Request("Almack96", "Alex", "Mackintosh", "alexmackintosh96@gmail.com", UserRole.Administrator);

        var response = await AsAdmin().PostAsJsonAsync("/api/v1/users", request, ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await DeserializeAsync<UserDto>(response, ct);

        Assert.NotNull(result);
        Assert.Equal("Almack96", result.Username);

        // Retrieve the newly created user directly from the database context
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PremPointsDbContext>();

        var createdUser = await context.Users
            .SingleOrDefaultAsync(u => u.Username == "Almack96", ct);

        // Check if the user exists in the database
        Assert.NotNull(createdUser);

        // Check that all critical fields were correctly persisted
        Assert.Equal("Alex", createdUser.FirstName);
        Assert.Equal("Mackintosh", createdUser.LastName);
        Assert.Equal("alexmackintosh96@gmail.com", createdUser.Email);
        Assert.Equal(UserRole.Administrator, createdUser.Role);
    }
}
