using Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Api.Features.Trades;

public static class TradeMapper
{
    public static TradeDto ToDto(this TradeEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return new TradeDto
        {
            Id = entity.Id,
            Exposure = entity.Exposure,
            SeasonPeriodId = entity.SeasonPeriodId,
            UserId = entity.UserId,
            PriceId = entity.PriceId,
            TeamId = entity.TeamId,
            TradeType = entity.TradeType,
            TradeDateUtc = entity.TradeDateUtc,
            TimezoneIana = entity.TimezoneIana
        };
    }
    public static TradeEntity ToEntity(this TradeDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        return new TradeEntity
        {
            Id = dto.Id,
            Exposure = dto.Exposure,
            SeasonPeriodId = dto.SeasonPeriodId,
            UserId = dto.UserId,
            PriceId = dto.PriceId,
            TeamId = dto.TeamId,
            TradeType = dto.TradeType,
            TradeDateUtc = dto.TradeDateUtc,
            TimezoneIana = dto.TimezoneIana
        };
    }
    public static List<TradeDto> ToDtos(this List<TradeEntity> entities) => entities.Select(ToDto).ToList();
    public static List<TradeEntity> ToEntities(this List<TradeDto> dtos) => dtos.Select(ToEntity).ToList();
}
