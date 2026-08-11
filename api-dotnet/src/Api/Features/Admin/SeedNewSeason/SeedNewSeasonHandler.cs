using Api.Domain.Entities;
using Api.Infrastructure.EntityFramework;
using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static Api.Features.Admin.SeedNewSeason.SeedNewSeason;

namespace Api.Features.Admin.SeedNewSeason;

public class SeedNewSeasonHandler(PremPointsDbContext context) : IRequestHandler<Command, Result>
{
    public async Task<Result> Handle(Command command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var newSeason = new SeasonEntity { SeasonName = command.SeasonName, StartYear = GetStartYear(command.StartDate) };

        context.Seasons.Add(newSeason);

        await context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private static int GetStartYear(DateOnly StartDate) => StartDate.Year;
}
