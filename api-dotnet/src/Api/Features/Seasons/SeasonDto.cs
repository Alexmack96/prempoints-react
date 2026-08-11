using Api.Domain.Contracts;

namespace Api.Features.Seasons;

public record SeasonDto : IEntityDto
{
    public Guid Id { get; init; }
    public required string SeasonName { get; init; }
    public int StartYear { get; init; }
}