import { useQuery } from '@tanstack/react-query';
import { apiClient } from '../../lib/api';
import type { PagedResponse } from '../../lib/pagedResponse';
import type { LeaderboardRowDto } from './leaderboardDto';

/**
 * The season standings: every enrolled player, best first.
 *
 * Ranking and ordering are the server's, not this hook's — two clients sorting
 * the same rows differently is exactly the disagreement a leaderboard cannot
 * afford.
 */
export const useLeaderboard = () =>
  useQuery({
    queryKey: ['leaderboard'],
    queryFn: async ({ signal }) => {
      const response = await apiClient.get<PagedResponse<LeaderboardRowDto>>('/leaderboard', {
        params: { pageSize: 100 },
        signal,
      });

      return response.data.items;
    },
  });
