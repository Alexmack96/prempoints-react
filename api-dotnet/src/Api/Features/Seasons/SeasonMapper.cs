using Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Api.Features.Seasons;

public static class SeasonMapper
{
    public static SeasonDto ToDto(this SeasonEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return new SeasonDto { Id = entity.Id, SeasonName = entity.SeasonName, StartYear = entity.StartYear };
    }
    public static SeasonEntity ToEntity(this SeasonDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        return new SeasonEntity { Id = dto.Id, SeasonName = dto.SeasonName, StartYear = dto.StartYear };
    }
    public static List<SeasonDto> ToDtos(this List<SeasonEntity> entities) => entities.Select(ToDto).ToList();
    public static List<SeasonEntity> ToEntities(this List<SeasonDto> dtos) => dtos.Select(ToEntity).ToList();
}
