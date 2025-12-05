export const teamKeys = {
  // 1. The Root Key (everything related to teams)
  all: ['teams'] as const,

  // 2. The List Key (for arrays of teams)
  // Usage: teamKeys.lists()
  lists: () => [...teamKeys.all, 'list'] as const,

  // 3. The Detail Key (for a specific team)
  // Usage: teamKeys.details('123')
  details: (id: string) => [...teamKeys.all, 'detail', id] as const,
};
