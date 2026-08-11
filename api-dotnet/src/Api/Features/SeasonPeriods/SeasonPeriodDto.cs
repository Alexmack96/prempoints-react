using Api.Domain.Contracts;

namespace Api.Features.SeasonPeriods;

public record SeasonPeriodDto : IEntityDto
{
    public Guid Id { get; init; }
    public Guid SeasonId { get; init; }
    public required DateOnly PeriodStartDate { get; init; }
    public required DateOnly PeriodEndDate { get; init; }
    public required int GameweekNumber { get; init; }
}