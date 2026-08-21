export type UserDto = {
  id: string;
  workOSUserId: string;
  username: string;
  firstName: string;
  lastName: string;
  email: string;
  role: 'Standard' | 'Administrator';
};
