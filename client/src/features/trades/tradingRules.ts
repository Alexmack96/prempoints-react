/**
 * The rules of the game, mirroring api-dotnet Features/Trades/TradingRules.cs.
 *
 * Duplicated so the board can guide a player as they pick rather than rejecting
 * the whole submission at the end. The server copy is the one that decides —
 * this one only decides what the UI lets you try.
 */
export const TRADING_RULES = {
  /** Every submission stakes exactly this much: |X| + |Y| = 40. */
  totalStake: 40,
  stakeIncrement: 5,
  minStake: 5,
  maxPositions: 2,
} as const;

/** Absolute, because a short is a position of the same size as a long. */
export const totalStaked = (amounts: number[]) =>
  amounts.reduce((total, amount) => total + Math.abs(amount), 0);
