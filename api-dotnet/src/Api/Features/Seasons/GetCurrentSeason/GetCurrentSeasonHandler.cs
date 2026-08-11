using Api.Infrastructure.EntityFramework;
using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static Api.Features.Seasons.GetCurrentSeason.GetCurrentSeason;

namespace Api.Features.Seasons.GetCurrentSeason;

public class GetCurrentSeasonHandler(ILogger<GetCurrentSeasonHandler> logger, PremPointsDbContext context) : IRequestHandler<Query, Result<SeasonDto>>
{
    public async Task<Result<SeasonDto>> Handle(Query query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var requestedDate = query.AsAtDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var currentSeason = await SeasonQueries.GetByDateAsync(context, requestedDate, cancellationToken);

        if (currentSeason is null)
        {
            logger.LogWarning("Unable to find valid period as at date: {AsAtDate}", query.AsAtDate);
            return Result.NotFound();
        }

        return currentSeason.ToDto();
    }
}
