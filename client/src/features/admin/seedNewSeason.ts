import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { apiClient } from '../../lib/api';
import type { SeasonDto } from '../seasons/seasonDto';

export type SeedNewSeasonRequest = {
  seasonName: string;
  startDate: string;
  endDate: string;
  promotedTeams: string[];
  relegatedTeams: string[];
};

export type SeedNewSeasonResult = {
  seasonId: string;
  seasonName: string;
  startYear: number;
  gameweeksCreated: number;
  teamsCreated: string[];
  teamsEnrolled: string[];
};

/**
 * Whether a season already covers the given date.
 *
 * The seed endpoint refuses a second season for the same start year, so this is
 * not what prevents a double run — it is what lets the page say so before the
 * admin fills in twenty club names and presses the button.
 */
export const useSeasonCovering = (date: string | null) =>
  useQuery({
    queryKey: ['seasons', 'covering', date],
    enabled: Boolean(date),
    queryFn: async ({ signal }) => {
      try {
        const response = await apiClient.get<SeasonDto>('/seasons/current', {
          params: { asAtDate: date },
          signal,
        });
        return response.data;
      } catch (error) {
        // 404 is the answer "no season covers that date", which is the normal
        // case before seeding. Anything else is a real failure worth surfacing.
        if (
          error !== null &&
          typeof error === 'object' &&
          'response' in error &&
          (error as { response?: { status?: number } }).response?.status === 404
        ) {
          return null;
        }
        throw error;
      }
    },
    retry: false,
  });

export const useSeedNewSeason = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (request: SeedNewSeasonRequest) => {
      const response = await apiClient.post<SeedNewSeasonResult>('/seednewseason', request);
      return response.data;
    },
    onSuccess: () => {
      // Seeding creates the season, its gameweeks and the whole club roster, so
      // every list on the page is stale at once.
      queryClient.invalidateQueries({ queryKey: ['seasons'] });
      queryClient.invalidateQueries({ queryKey: ['teams'] });
    },
  });
};
