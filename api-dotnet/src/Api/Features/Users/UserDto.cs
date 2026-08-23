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
    public bool UsernameChosen { get; init; }
    public Guid? FavouriteTeamId { get; init; }

    /// Denormalised for display: the client draws a badge from the club's name,
    /// and making it fetch the whole team list to turn an id into a name would
    /// be a second request for one string.
    public string? FavouriteTeamName { get; init; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public UserRole Role { get; init; }
}

