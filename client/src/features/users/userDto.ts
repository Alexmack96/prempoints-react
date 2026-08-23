export type UserDto = {
  id: string;
  workOSUserId: string;
  username: string;
  firstName: string;
  lastName: string;
  /**
   * False until the player has been through the onboarding gate. It is what
   * tells a generated username from a chosen one, so the app knows whether to
   * ask. Keeping the generated name counts as choosing it.
   */
  usernameChosen: boolean;
  favouriteTeamId: string | null;
  favouriteTeamName: string | null;
  role: 'Standard' | 'Administrator';
};
