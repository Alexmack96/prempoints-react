import { useMutation, useQueryClient } from '@tanstack/react-query';
import { apiClient } from '../../../lib/api';
import { teamKeys } from '../teamKeys';
import type { TeamDto } from '../teamDto';

// 1. Types
type CreateTeamRequest = { teamName: string };

// 2. API (Colocated)
const createTeam = async (newTeam: CreateTeamRequest) => {
  // 201 Created; the Location header points at the new team.
  return apiClient.post<TeamDto>('/teams', newTeam);
};

// 3. Hook
export const useCreateTeam = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: createTeam,
    onSuccess: () => {
      // Invalidate using the shared key factory
      queryClient.invalidateQueries({ queryKey: teamKeys.lists() });
    },
  });
};
