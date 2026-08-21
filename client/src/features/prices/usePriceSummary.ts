import { useQuery } from '@tanstack/react-query';
import { apiClient } from '../../lib/api';
import type { PagedResponse } from '../../lib/pagedResponse';
import type { TeamPriceSummaryDto } from './priceSummaryDto';

/**
 * Every club with its latest quote, highest first. The join and the movement
 * comparison happen server-side so each client does not have to get them right
 * independently.
 */
export const usePriceSummary = () =>
  useQuery({
    queryKey: ['prices', 'summary'],
    queryFn: async ({ signal }) => {
      const response = await apiClient.get<PagedResponse<TeamPriceSummaryDto>>('/prices/summary', {
        params: { pageSize: 100 },
        signal,
      });

      return response.data.items;
    },
  });
