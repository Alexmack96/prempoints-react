import { useMutation } from '@tanstack/react-query';
import { apiClient } from '../../lib/api';
import { systemClock, type Clock } from '../../lib/clock';

/** Matches the API's TradeType. Joker is the 2x multiplier. */
export type TradeType = 'Standard' | 'Joker' | 'ManagerOfTheMonth';

export type SubmitTradesRequest = {
  /** Team name to signed exposure. Negative is a short. */
  exposuresByTeam: Record<string, number>;
  tradeType: TradeType;
};

export const useSubmitTrades = (clock: Clock = systemClock) =>
  useMutation({
    mutationFn: async ({ exposuresByTeam, tradeType }: SubmitTradesRequest) => {
      // No username in the body. The API takes the player from the bearer
      // token, so a client cannot submit trades as someone else by editing a
      // field.
      const response = await apiClient.post('/trades', {
        tradeDateUtc: clock.now(),
        // Persisted per trade, and read back by the PnL multiplier — a Joker
        // scores double.
        tradeType,
        timezoneIana: Intl.DateTimeFormat().resolvedOptions().timeZone,
        exposuresByTeam,
      });

      return response.data;
    },
  });
