import { useMutation, useQueryClient } from '@tanstack/react-query';
import { apiClient } from '../../lib/api';
import type { UserDto } from './userDto';

export type UpdateMyProfile = {
  username: string;
  favouriteTeamId: string | null;
};

/**
 * Sets the signed-in player's username and club.
 *
 * The API takes both in one PATCH, so a failure leaves neither applied. Sending
 * them separately could name someone and then fail to badge them, which is the
 * one state the onboarding gate exists to prevent.
 */
export const useUpdateMyProfile = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (profile: UpdateMyProfile) => {
      const response = await apiClient.patch<UserDto>('/users/me', profile);
      return response.data;
    },
    onSuccess: (user) => {
      // Seed the cache from the response rather than refetching. The gate
      // unmounts on `usernameChosen`, and a round trip here would leave it on
      // screen for another moment after the player has finished with it.
      queryClient.setQueryData(['users', 'me', user.workOSUserId], user);
      queryClient.invalidateQueries({ queryKey: ['users'] });
      queryClient.invalidateQueries({ queryKey: ['leaderboard'] });
    },
  });
};
