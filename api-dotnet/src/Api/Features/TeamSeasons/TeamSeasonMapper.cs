using Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Api.Features.TeamSeasons;

public static class TeamSeasonMapper
{
    public static TeamSeasonDto ToDto(this TeamSeasonEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return new TeamSeasonDto
        {
            Id = entity.Id,
            SeasonId = entity.SeasonId,
            TeamId = entity.TeamId,
        };
    }

    public static TeamSeasonEntity ToEntity(this TeamSeasonDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        return new TeamSeasonEntity
        {
            Id = dto.Id,
            SeasonId = dto.SeasonId,
            TeamId = dto.TeamId,
        };
    }

    public static List<TeamSeasonDto> ToDtos(this List<TeamSeasonEntity> entities) => entities.Select(ToDto).ToList();
    public static List<TeamSeasonEntity> ToEntities(this List<TeamSeasonDto> dtos) => dtos.Select(ToEntity).ToList();
}
