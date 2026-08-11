using Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Api.Features.Teams;

public static class TeamMapper
{
    public static TeamDto ToDto(this TeamEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return new TeamDto { Id = entity.Id, TeamName = entity.TeamName };
    }
    public static TeamEntity ToEntity(this TeamDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        return new TeamEntity { Id = dto.Id, TeamName = dto.TeamName };
    }
    public static List<TeamDto> ToDtos(this List<TeamEntity> entities) => entities.Select(ToDto).ToList();
    public static List<TeamEntity> ToEntities(this List<TeamDto> dtos) => dtos.Select(ToEntity).ToList();
}
