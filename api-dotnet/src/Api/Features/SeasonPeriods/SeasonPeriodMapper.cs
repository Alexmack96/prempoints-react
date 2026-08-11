using Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Api.Features.SeasonPeriods;

public static class SeasonPeriodMapper
{
    public static SeasonPeriodDto ToDto(this SeasonPeriodEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return new SeasonPeriodDto { Id = entity.Id, PeriodStartDate = entity.PeriodStartDate, PeriodEndDate = entity.PeriodEndDate, SeasonId = entity.SeasonId, GameweekNumber = entity.GameweekNumber };
    }
    public static SeasonPeriodEntity ToEntity(this SeasonPeriodDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        return new SeasonPeriodEntity { Id = dto.Id, PeriodStartDate = dto.PeriodStartDate, PeriodEndDate = dto.PeriodEndDate, SeasonId = dto.SeasonId, GameweekNumber = dto.GameweekNumber };
    }
    public static List<SeasonPeriodDto> ToDtos(this List<SeasonPeriodEntity> entities) => entities.Select(ToDto).ToList();
    public static List<SeasonPeriodEntity> ToEntities(this List<SeasonPeriodDto> dtos) => dtos.Select(ToEntity).ToList();
}
