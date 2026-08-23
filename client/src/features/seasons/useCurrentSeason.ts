import { useQuery } from '@tanstack/react-query';
import { apiClient } from '../../lib/api';
import type { SeasonDto } from './seasonDto';

/**
 * The season covering today, as the API sees it.
 *
 * Anonymous, so it resolves on the sign-in page too — the header renders before
 * anyone has a token.
 */
export const useCurrentSeason = () =>
  useQuery({
    queryKey: ['seasons', 'current'],
    queryFn: async ({ signal }) => {
      const response = await apiClient.get<SeasonDto>('/seasons/current', { signal });
      return response.data;
    },
    // A season changes once a year. Refetching it on every window focus is
    // noise, and the value is the same all afternoon.
    staleTime: 1000 * 60 * 60,
    // Out of season the API answers 404, which is an answer rather than a
    // failure. Retrying it three times just delays the fallback label.
    retry: false,
  });

/**
 * The season's name, or null while it is unknown.
 *
 * Null covers three states that all want the same treatment: still loading, no
 * season covers today, and the API is unreachable. Callers render something
 * without a year rather than "undefined standings" — which is exactly what this
 * app showed the day the seeded season ran out.
 */
export const useSeasonLabel = (): string | null => {
  const { data } = useCurrentSeason();

  return data?.seasonName ?? null;
};
