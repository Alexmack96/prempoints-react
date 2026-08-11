using Api.Domain.Contracts;

namespace Api.Features.Teams;

public record TeamDto : IEntityDto
{
    public Guid Id { get; init; }
    public required string TeamName { get; init; }
}