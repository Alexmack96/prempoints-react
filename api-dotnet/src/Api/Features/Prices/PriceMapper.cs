using Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Api.Features.Prices;

public static class PriceMapper
{
    public static PriceDto ToDto(this PriceEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return new PriceDto { Id = entity.Id, TeamId = entity.TeamId, SeasonPeriodId = entity.SeasonPeriodId, Price = entity.Price, ValueDate = entity.ValueDate };
    }
    public static PriceEntity ToEntity(this PriceDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        return new PriceEntity { Id = dto.Id, TeamId = dto.TeamId, SeasonPeriodId = dto.SeasonPeriodId, Price = dto.Price, ValueDate = dto.ValueDate };
    }
    public static List<PriceDto> ToDtos(this List<PriceEntity> entities) => entities.Select(ToDto).ToList();
    public static List<PriceEntity> ToEntities(this List<PriceDto> dtos) => dtos.Select(ToEntity).ToList();
}
