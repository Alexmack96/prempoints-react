import { useQuery } from '@tanstack/react-query';
import { useAuth } from '@workos-inc/authkit-react';
import { apiClient } from '../../lib/api';
import { systemClock, type Clock } from '../../lib/clock';

export type JokerUse = {
  calendarYear: number;
  playedOnUtc: string;
};

export type JokerAllowance = {
  seasonId: string;
  seasonName: string;
  calendarYear: number;
  /** Whether a joker may be played on the requested date. */
  available: boolean;
  /** The joker blocking this date, if one does. */
  blockedByUtc: string | null;
  playedThisSeason: JokerUse[];
};

/**
 * Whether the signed-in player still has a joker for today.
 *
 * Asked up front so the checkbox can be disabled with a reason, rather than
 * letting someone decide to play it and only then be refused.
 */
export const useJokerAllowance = (clock: Clock = systemClock) => {
  const { user, isLoading } = useAuth();

  return useQuery({
    queryKey: ['trades', 'joker-allowance', user?.id],
    enabled: !isLoading && Boolean(user),
    queryFn: async ({ signal }) => {
      const response = await apiClient.get<JokerAllowance>('/trades/joker-allowance', {
        params: { tradeDateUtc: clock.now() },
        signal,
      });

      return response.data;
    },
  });
};
