using Api.Domain.Entities;
using Api.Features.Teams;
using Api.Features.Teams.GetActiveTeams;
using Api.Infrastructure.EntityFramework;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace IntegrationTests.Features;

//Example of where you might need extra data than the base data!
public class GetActiveTeamsTests : BaseIntegrationTest
{
    private const string BaseUrl = "/api/teams/active";

    [Fact]
    public async Task GetActiveTeams_ReturnsOk_AndGetsCorrectTeams()
    {
        //Arrange
        var ct = TestContext.Current.CancellationToken;
        var asAtDate = new DateOnly(2025, 08, 15);

        using var scope = Factory.Services.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<PremPointsDbContext>();

        // Add 10 extra teams to prove we can do it in the test method
        for (int i = 0; i < 10; i++)
        {
            context.Teams.Add(new TeamEntity { Id = Guid.CreateVersion7(), TeamName = $"TeamExtra{i}" });
        }
        await context.SaveChangesAsync(ct);
        Dictionary<string, string> parameters = new();
        parameters[nameof(asAtDate)] = asAtDate.ToString("O");
        var request = new GetActiveTeams.Request(asAtDate);

        var uri = BaseUrl.AddQueryString(parameters);

        //Act
        var response = await HttpClient.GetAsync(uri, ct);

        //Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<List<TeamDto>>(ct);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetActiveTeams_ReturnsCritical_WhenNoActiveTeamsInValidPeriod()
    {
        var ct = TestContext.Current.CancellationToken;


        //Arrange
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PremPointsDbContext>();
        var allTeamSeasons = context.TeamSeasons.ToList();
        context.TeamSeasons.RemoveRange(allTeamSeasons);
        await context.SaveChangesAsync(ct);

        var asAtDate = new DateOnly(2025, 08, 15);
        Dictionary<string, string> parameters = new();
        parameters[nameof(asAtDate)] = asAtDate.ToString("O");
        var request = new GetActiveTeams.Request(asAtDate);
        var uri = BaseUrl.AddQueryString(parameters);
        var response = await HttpClient.GetAsync(uri, ct);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

}
