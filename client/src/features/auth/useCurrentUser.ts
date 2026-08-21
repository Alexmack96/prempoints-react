import { useQuery } from '@tanstack/react-query';
import { useAuth } from '@workos-inc/authkit-react';
import { apiClient } from '../../lib/api';
import type { UserDto } from '../users/userDto';

/**
 * The signed-in player as this application knows them.
 *
 * AuthKit's `user` is the WorkOS identity; this is the PremPoints row it maps
 * to, which is what carries the username and role. Someone can be signed in
 * with WorkOS and have no account here, so this can 404 while auth is fine.
 */
export const useCurrentUser = () => {
  const { user, isLoading } = useAuth();

  return useQuery({
    queryKey: ['users', 'me', user?.id],
    enabled: !isLoading && Boolean(user),
    queryFn: async ({ signal }) => {
      const response = await apiClient.get<UserDto>('/users/me', { signal });
      return response.data;
    },
  });
};
