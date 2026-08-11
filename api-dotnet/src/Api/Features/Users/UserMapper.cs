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
            Email = entity.Email,
            FirstName = entity.FirstName,
            LastName = entity.LastName,
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
            Email = dto.Email,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Role = dto.Role
        };
    }
    public static List<UserDto> ToDtos(this List<UserEntity> entities) => entities.Select(ToDto).ToList();
    public static List<UserEntity> ToEntities(this List<UserDto> dtos) => dtos.Select(ToEntity).ToList();
}
