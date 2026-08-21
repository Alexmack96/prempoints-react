import { useQuery } from '@tanstack/react-query';
import { teamKeys } from '../teamKeys';
import { apiClient } from '../../../lib/api';
import { systemClock, type Clock } from '../../../lib/clock';
import type { PagedResponse } from '../../../lib/pagedResponse';
import type { TeamDto } from '../teamDto';

// The 20 Premier League clubs fit in one page. Asked for explicitly so the
// default page size can change without this list silently truncating.
const PAGE_SIZE = 100;

export const getTeams = async (signal: AbortSignal, clock: Clock): Promise<TeamDto[]> => {
  const response = await apiClient.get<PagedResponse<TeamDto>>('/teams', {
    params: {
      // "Active" is a filter on the teams collection rather than a route of its
      // own, and the date is supplied rather than assumed by the server, so the
      // same request replayed on another day asks the same question.
      activeOn: clock.today(),
      pageSize: PAGE_SIZE,
    },
    signal: signal,
  });

  return response.data.items;
};

export const useTeamsList = (clock: Clock = systemClock) => {
  return useQuery({
    queryKey: teamKeys.lists(),
    queryFn: ({ signal }) => getTeams(signal, clock),
  });
};
