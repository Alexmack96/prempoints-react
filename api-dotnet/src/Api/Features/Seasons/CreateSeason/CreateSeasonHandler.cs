using Api.Domain.Entities;
using Api.Infrastructure.EntityFramework;
using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static Api.Features.Seasons.CreateSeason.CreateSeason;

namespace Api.Features.Seasons.CreateSeason;

public class CreateSeasonHandler(PremPointsDbContext context, TimeProvider clock) : IRequestHandler<Command, Result<SeasonDto>>
{
    public async Task<Result<SeasonDto>> Handle(Command command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        // StartYear carries a unique index, and CreateSeasonPeriod looks seasons
        // up by it, so a duplicate year is as much a conflict as a duplicate
        // name — catching both here keeps that index from surfacing as a 500.
        var seasonExists = await context.Seasons.AnyAsync(
            s => s.SeasonName == command.SeasonName || s.StartYear == command.StartYear,
            cancellationToken);

        if (seasonExists)
            return Result.Conflict("DuplicateName", $"Season '{command.SeasonName}' already exists.");

        // Ids are ValueGenerated.Never for auditable entities (see
        // PremPointsDbContext.OnModelCreating), so the handler owns this.
        var entity = new SeasonEntity
        {
            Id = Guid.CreateVersion7(clock.GetUtcNow()),
            SeasonName = command.SeasonName,
            StartYear = command.StartYear,
        };

        context.Seasons.Add(entity);

        await context.SaveChangesAsync(cancellationToken);

        return entity.ToDto();
    }
}
