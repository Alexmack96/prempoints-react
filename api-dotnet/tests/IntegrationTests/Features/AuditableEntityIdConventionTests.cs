using Api.Domain.Entities;
using Api.Infrastructure.EntityFramework;
using Microsoft.Extensions.DependencyInjection;

namespace IntegrationTests.Features;

/// Guards the convention that every auditable entity carries an id its handler
/// generated. Auditable Guid ids are ValueGenerated.Never, so a forgotten id is
/// silently inserted as Guid.Empty — the first row per table saves, the second
/// collides on the primary key, and the failure surfaces nowhere near the
/// handler at fault.
public class AuditableEntityIdConventionTests : BaseIntegrationTest
{
    [Fact]
    public async Task SaveChanges_Throws_WhenAnAuditableEntityHasNoId()
    {
        var ct = TestContext.Current.CancellationToken;

        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PremPointsDbContext>();

        // No Id: exactly what the five broken handlers were doing.
        context.Teams.Add(new TeamEntity { TeamName = "TeamWithNoId" });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.SaveChangesAsync(ct));

        Assert.Contains("empty Id", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SaveChanges_Succeeds_WhenTheIdIsSet()
    {
        var ct = TestContext.Current.CancellationToken;

        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PremPointsDbContext>();

        context.Teams.Add(new TeamEntity { Id = Guid.CreateVersion7(), TeamName = "TeamWithAnId" });

        await context.SaveChangesAsync(ct);
    }
}
