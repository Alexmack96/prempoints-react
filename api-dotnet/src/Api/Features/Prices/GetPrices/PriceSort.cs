using Api.Domain.Entities;
using Api.Infrastructure.Paging;
using System.Linq.Expressions;

namespace Api.Features.Prices.GetPrices;

/// <summary>
/// What GET /prices may be sorted by. Mid is included because it is a real
/// column — a persisted computed one — so the database can order on it.
/// </summary>
public static class PriceSort
{
    public static readonly SortMap<PriceEntity> Map = new(
        defaultKey: "valueDate",
        keys: new Dictionary<string, Expression<Func<PriceEntity, object?>>>(StringComparer.OrdinalIgnoreCase)
        {
            ["valueDate"] = price => price.ValueDate,
            ["mid"] = price => price.Mid,
            ["teamName"] = price => price.Team.TeamName,
        });
}
