using Api.Domain.Entities;
using Api.Infrastructure.EntityFramework;
using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static Api.Features.Seasons.CreateSeason.CreateSeason;

namespace Api.Features.Seasons.CreateSeason;

public class CreateSeasonHandler(PremPointsDbContext context) : IRequestHandler<Command, Result<SeasonDto>>
{
    public async Task<Result<SeasonDto>> Handle(Command command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var seasonExists = await context.Teams.AnyAsync(t => t.TeamName == command.SeasonName, cancellationToken);
        if (seasonExists)
            return Result.Conflict("DuplicateName", $"Season '{command.SeasonName}' already exists.");

        var entity = new SeasonEntity { SeasonName = command.SeasonName, StartYear = command.StartYear };

        context.Seasons.Add(entity);

        await context.SaveChangesAsync(cancellationToken);

        return entity.ToDto();
    }
}
