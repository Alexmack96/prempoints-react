export type LeaderboardRowDto = {
  /** 1-based position. Equal scores share a rank, so ranks repeat. */
  rank: number;
  userId: string;
  username: string;
  firstName: string;
  lastName: string;
  pnl: number;
  tradesPlaced: number;
  /**
   * False while trades are not yet marked against a settled price. The zero in
   * `pnl` is a placeholder then, not a result, and the page says so rather than
   * presenting it as a score.
   */
  pnlIsSettled: boolean;
};
