using Api.Infrastructure.EntityFramework;
using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static Api.Features.Trades.PatchTradeTypes.PatchTradeTypes;

namespace Api.Features.Trades.PatchTradeTypes;

public class PatchTradeTypesHandler(PremPointsDbContext context) : IRequestHandler<Command, Result<List<TradeDto>>>
{
    public async Task<Result<List<TradeDto>>> Handle(Command command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var trades = await context.Trades
                .Where(t => command.TradeIds.Contains(t.Id))
                .ToListAsync(cancellationToken);

        if (trades.Count != command.TradeIds.Count)
        {
            var foundIds = trades.Select(t => t.Id).ToHashSet();
            var missingIds = command.TradeIds.Where(id => !foundIds.Contains(id));

            return Result.NotFound($"The following Trade IDs were not found: {string.Join(", ", missingIds)}");
        }

        foreach (var trade in trades)
        {
            trade.TradeType = command.TradeType;
        }

        await context.SaveChangesAsync(cancellationToken);

        return trades.ToDtos();
    }
}
