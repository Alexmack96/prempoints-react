using Api.Domain.Contracts;

namespace Api.Features.TeamSeasons;

public record TeamSeasonDto : IEntityDto
{
    public Guid Id { get; init; }
    public Guid TeamId { get; init; }
    public Guid SeasonId { get; init; }
}