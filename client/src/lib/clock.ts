/**
 * The clock, as an input.
 *
 * Code that reads the current time directly cannot be tested at a chosen
 * instant, so anything needing "now" takes one of these. Tests supply a fixed
 * clock; production passes `systemClock`.
 */
export type Clock = {
  /** ISO yyyy-mm-dd, which is what the API's DateOnly parameters expect. */
  today: () => string;
  /** Full ISO instant, for the API's DateTime parameters. */
  now: () => string;
};

export const systemClock: Clock = {
  today: () => new Date().toISOString().slice(0, 10),
  now: () => new Date().toISOString(),
};

export const fixedClock = (iso: string): Clock => ({
  today: () => iso.slice(0, 10),
  now: () => iso,
});
