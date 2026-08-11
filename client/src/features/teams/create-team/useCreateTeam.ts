import { useMutation, useQueryClient } from '@tanstack/react-query';
import { apiClient } from '../../../lib/api';
import { teamKeys } from '../teamKeys';

// 1. Types
type CreateTeamRequest = { teamName: string };

// 2. API (Colocated)
const createTeam = async (newTeam: CreateTeamRequest) => {
  return apiClient.post('teams', newTeam);
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
