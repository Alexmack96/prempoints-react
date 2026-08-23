using Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Api.Features.Users;

public static class UserMapper
{
    public static UserDto ToDto(this UserEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return new UserDto
        {
            Id = entity.Id,
            WorkOSUserId = entity.WorkOSUserId,
            Username = entity.Username,
            FirstName = entity.FirstName,
            LastName = entity.LastName,
            UsernameChosen = entity.UsernameChosen,
            FavouriteTeamId = entity.FavouriteTeamId,
            FavouriteTeamName = entity.FavouriteTeam?.TeamName,
            Role = entity.Role
        };
    }
    public static UserEntity ToEntity(this UserDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        return new UserEntity
        {
            Id = dto.Id,
            WorkOSUserId = dto.WorkOSUserId,
            Username = dto.Username,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            UsernameChosen = dto.UsernameChosen,
            FavouriteTeamId = dto.FavouriteTeamId,
            Role = dto.Role
        };
    }
    public static List<UserDto> ToDtos(this List<UserEntity> entities) => entities.Select(ToDto).ToList();
    public static List<UserEntity> ToEntities(this List<UserDto> dtos) => dtos.Select(ToEntity).ToList();
}
