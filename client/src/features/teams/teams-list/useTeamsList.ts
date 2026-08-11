import { useQuery } from '@tanstack/react-query';
import { teamKeys } from '../teamKeys';
import { apiClient } from '../../../lib/api';
import type { TeamDto } from '../teamDto';

export const getTeams = async (signal: AbortSignal): Promise<TeamDto[]> => {
  const response = await apiClient.get<TeamDto[]>('/teams/active', {
    signal: signal,
  });

  return response.data;
};

export const useTeamsList = () => {
  return useQuery({
    queryKey: teamKeys.lists(),
    queryFn: ({ signal }) => getTeams(signal),
  });
};
