using Api.Domain.Authorization;
using Api.Domain.Contracts;
using System.Text.Json.Serialization;

namespace Api.Features.Users;

public record UserDto : IEntityDto
{
    public Guid Id { get; init; }
    public required string WorkOSUserId { get; init; }
    public required string Username { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string Email { get; init; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public UserRole Role { get; init; }
}

