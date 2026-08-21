export type PriceMovement = 'Unknown' | 'Up' | 'Down' | 'Level';

export type TeamPriceSummaryDto = {
  teamId: string;
  teamName: string;
  /** Null until a price has been loaded for the club. */
  bid: number | null;
  ask: number | null;
  mid: number | null;
  valueDate: string | null;
  priceType: 'Provisional' | 'Final' | null;
  /** The mid before this one — what movement compares against. */
  previousMid: number | null;
  movement: PriceMovement;
};
