export type PriceDto = {
  id: string;
  teamId: string;
  seasonPeriodId: string;
  /** Sell side. */
  bid: number;
  /** Buy side. */
  ask: number;
  /** Midway between the two — the number we quote. Derived by the database. */
  mid: number;
  priceType: 'Provisional' | 'Final';
  valueDate: string;
};
