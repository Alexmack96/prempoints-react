import { useQuery } from '@tanstack/react-query';
import { teamKeys } from '../teamKeys';
import { apiClient } from '../../../lib/api';
import { systemClock, type Clock } from '../../../lib/clock';
import type { PagedResponse } from '../../../lib/pagedResponse';
import type { TeamDto } from '../teamDto';

// The 20 Premier League clubs fit in one page. Asked for explicitly so the
// server's default page size can change without this list silently truncating.
const PAGE_SIZE = 100;

const fetchTeams = async (signal: AbortSignal, activeOn?: string) => {
  const response = await apiClient.get<PagedResponse<TeamDto>>('/teams', {
    params: { activeOn, pageSize: PAGE_SIZE },
    signal,
  });

  return response.data.items;
};

export const getTeams = async (signal: AbortSignal, clock: Clock): Promise<TeamDto[]> => {
  // "Active" is a filter on the teams collection rather than a route of its own,
  // and the date is supplied rather than assumed by the server, so the same
  // request replayed on another day asks the same question.
  const active = await fetchTeams(signal, clock.today());

  if (active.length > 0) {
    return active;
  }

  // No season covers today, so nothing is active and the API correctly returns
  // an empty page. That is the right answer to the question asked and the wrong
  // thing to show a player, who sees a blank board and assumes the site is
  // broken. Fall back to every team rather than nothing, and let them trade.
  return fetchTeams(signal);
};

export const useTeamsList = (clock: Clock = systemClock) =>
  useQuery({
    queryKey: teamKeys.lists(),
    queryFn: ({ signal }) => getTeams(signal, clock),
  });
