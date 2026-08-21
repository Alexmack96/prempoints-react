using Api.Infrastructure.EntityFramework;
using Api.Infrastructure.Paging;
using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static Api.Features.Prices.GetPrices.GetPrices;

namespace Api.Features.Prices.GetPrices;

public class GetPricesHandler(PremPointsDbContext context)
    : IRequestHandler<Query, Result<PagedResponse<PriceDto>>>
{
    public async Task<Result<PagedResponse<PriceDto>>> Handle(Query query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var filtered = context.Prices.AsNoTracking();

        if (query.ValueDate is { } valueDate)
        {
            filtered = filtered.Where(price => price.ValueDate == valueDate);
        }

        if (!string.IsNullOrWhiteSpace(query.TeamName))
        {
            filtered = filtered.Where(price => price.Team.TeamName == query.TeamName);
        }

        var totalCount = await filtered.CountAsync(cancellationToken);

        var items = await PriceSort.Map.Apply(filtered, query.Sort)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(price => new PriceDto
            {
                Id = price.Id,
                TeamId = price.TeamId,
                SeasonPeriodId = price.SeasonPeriodId,
                Bid = price.Bid,
                Ask = price.Ask,
                Mid = price.Mid,
                PriceType = price.PriceType,
                ValueDate = price.ValueDate,
            })
            .ToListAsync(cancellationToken);

        return PagedResponse<PriceDto>.Create(items, query.Page, query.PageSize, totalCount);
    }
}
