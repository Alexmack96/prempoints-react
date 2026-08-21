using Api.Domain.Entities;

namespace Api.Features.Prices;

public static class PriceMapper
{
    public static PriceDto ToDto(this PriceEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new PriceDto
        {
            Id = entity.Id,
            TeamId = entity.TeamId,
            SeasonPeriodId = entity.SeasonPeriodId,
            Bid = entity.Bid,
            Ask = entity.Ask,
            Mid = entity.Mid,
            PriceType = entity.PriceType,
            ValueDate = entity.ValueDate,
        };
    }

    // There is deliberately no ToEntity. Mid is computed by the database, so a
    // round-trip through a DTO cannot reconstruct a price entity faithfully —
    // and nothing needed one.

    public static List<PriceDto> ToDtos(this List<PriceEntity> entities) => [.. entities.Select(ToDto)];
}
