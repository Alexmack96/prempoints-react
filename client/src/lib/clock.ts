/**
 * The clock, as an input.
 *
 * Code that reads the current date directly cannot be tested at a chosen
 * instant, so anything that needs "today" takes one of these instead. Tests
 * supply a fixed clock; production passes `systemClock`.
 */
export type Clock = {
  today: () => string;
};

export const systemClock: Clock = {
  // ISO yyyy-mm-dd, which is what the API's DateOnly parameters expect.
  today: () => new Date().toISOString().slice(0, 10),
};

export const fixedClock = (isoDate: string): Clock => ({
  today: () => isoDate,
});
