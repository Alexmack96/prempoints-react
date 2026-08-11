using Api.Domain.Contracts;

namespace Api.Features.UserSeasons;

public record UserSeasonDto : IEntityDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public Guid SeasonId { get; init; }
    public int LateJoinerFee { get; init; }

}