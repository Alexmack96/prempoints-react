import { useQuery } from '@tanstack/react-query';
import { apiClient } from '../../lib/api';
import { systemClock, type Clock } from '../../lib/clock';
import type { PagedResponse } from '../../lib/pagedResponse';
import type { PriceDto } from './priceDto';

/**
 * Today's price board, keyed by team id so a card can look up its own quote.
 *
 * Returns an empty map rather than failing when no prices are loaded — a board
 * with no quotes is a normal state at the start of a gameweek, and the cards
 * handle a missing price by saying so.
 */
export const useTodaysPrices = (clock: Clock = systemClock) =>
  useQuery({
    queryKey: ['prices', 'today'],
    queryFn: async ({ signal }) => {
      const response = await apiClient.get<PagedResponse<PriceDto>>('/prices', {
        params: { valueDate: clock.today(), pageSize: 100 },
        signal,
      });

      return new Map(response.data.items.map((price) => [price.teamId, price]));
    },
  });
