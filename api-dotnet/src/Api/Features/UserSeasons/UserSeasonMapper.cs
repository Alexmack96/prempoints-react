using Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Api.Features.UserSeasons;

public static class UserSeasonMapper
{
    public static UserSeasonDto ToDto(this UserSeasonEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return new UserSeasonDto
        {
            Id = entity.Id,
            SeasonId = entity.SeasonId,
            UserId = entity.UserId,
            LateJoinerFee = entity.LateJoinerFee,
        };
    }

    public static UserSeasonEntity ToEntity(this UserSeasonDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        return new UserSeasonEntity
        {
            Id = dto.Id,
            SeasonId = dto.SeasonId,
            UserId = dto.UserId,
            LateJoinerFee = dto.LateJoinerFee,
        };
    }

    public static List<UserSeasonDto> ToDtos(this List<UserSeasonEntity> entities) => entities.Select(ToDto).ToList();
    public static List<UserSeasonEntity> ToEntities(this List<UserSeasonDto> dtos) => dtos.Select(ToEntity).ToList();
}
