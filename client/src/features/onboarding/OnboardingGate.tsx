import { useState } from 'react';
import { Check, Loader2 } from 'lucide-react';
import { cn } from '@/lib/utils';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Skeleton } from '@/components/ui/skeleton';
import { useCurrentUser } from '../auth/useCurrentUser';
import { useTeamsList } from '../teams/teams-list/useTeamsList';
import { useUpdateMyProfile } from '../users/useUpdateMyProfile';
import { TeamBadge } from '../trades/TeamBadge';
import image_prem_lion from '../../assets/prem_lion.jpg';

const USERNAME_PATTERN = /^[A-Za-z0-9_-]+$/;

/**
 * Asked once, the first time someone signs in: what they want to be called and
 * whose badge sits beside their name.
 *
 * It renders instead of the app rather than over it. A player who has not
 * finished this has an auto-generated username on the leaderboard, so letting
 * them wander off mid-way is how that name becomes permanent by accident.
 *
 * Nothing here blocks sign-in. UserProvisioner has already created the row with
 * a usable name, so this is a chance to improve on it, not a form standing
 * between a player and their account.
 */
export const OnboardingGate = ({ children }: { children: React.ReactNode }) => {
  const { data: me, isLoading } = useCurrentUser();

  // Still resolving, or signed in with no player row — /users/me 404s until the
  // WorkOS JWT template is configured. Either way there is nothing to ask yet,
  // and the app behaves as it did before.
  if (isLoading || !me || me.usernameChosen) {
    return children;
  }

  return <OnboardingForm suggestedUsername={me.username} />;
};

const OnboardingForm = ({ suggestedUsername }: { suggestedUsername: string }) => {
  const [username, setUsername] = useState(suggestedUsername);
  const [teamId, setTeamId] = useState<string | null>(null);

  const { data: teams, isLoading: loadingTeams } = useTeamsList();
  const { mutate, isPending, error } = useUpdateMyProfile();

  const trimmed = username.trim();
  const tooShort = trimmed.length < 3;
  const badCharacters = username.length > 0 && !USERNAME_PATTERN.test(username);

  // The API answers 409 with a ProblemDetails when the name is taken. Shown
  // against the field, because that is the one thing the player can act on.
  const conflict =
    error !== null &&
    typeof error === 'object' &&
    'response' in error &&
    (error as { response?: { status?: number } }).response?.status === 409;

  const invalid = tooShort || badCharacters || conflict;
  const selectedTeam = teams?.find((team) => team.id === teamId) ?? null;

  const hint = conflict
    ? `${trimmed} is already taken.`
    : badCharacters
      ? 'Letters, digits, hyphens and underscores only.'
      : tooShort
        ? 'At least three characters.'
        : `We picked ${suggestedUsername} for you. Keep it or change it.`;

  return (
    <div className="mx-auto flex w-full max-w-xl flex-col gap-8 py-4">
      <header className="flex flex-col items-center gap-4 text-center">
        <span className="from-primary/70 to-primary/20 rounded-full bg-gradient-to-br p-[3px]">
          <img
            src={image_prem_lion}
            alt=""
            className="border-background size-16 rounded-full border-2 object-cover"
          />
        </span>
        <div className="space-y-1.5">
          <h1 className="text-2xl font-semibold tracking-tight">Welcome to PremPoints</h1>
          <p className="text-muted-foreground text-sm">
            Two quick things and you are in.
          </p>
        </div>
      </header>

      {/* The reason both questions sit on one screen: this is what the answers
          add up to. Seeing the row assemble as you type is worth more than any
          amount of explaining what the fields are for. */}
      <div className="glass border-border/60 flex items-center gap-3 rounded-2xl border p-4">
        <span className="text-muted-foreground w-6 text-center text-sm font-semibold">1</span>
        {selectedTeam ? (
          <TeamBadge teamName={selectedTeam.teamName} size={36} />
        ) : (
          <span className="border-border/60 size-9 shrink-0 rounded-full border border-dashed" />
        )}
        <span className="min-w-0 flex-1">
          <span className="block truncate text-sm font-medium">
            {trimmed.length > 0 ? trimmed : suggestedUsername}
          </span>
          <span className="text-muted-foreground block text-[11px]">
            {selectedTeam ? selectedTeam.teamName : 'No club yet'}
          </span>
        </span>
        <span className="numeric text-muted-foreground text-sm">0.0</span>
      </div>

      <div className="space-y-2">
        <Label htmlFor="username">Your name on the leaderboard</Label>
        <Input
          id="username"
          value={username}
          onChange={(event) => setUsername(event.target.value)}
          autoComplete="off"
          autoCapitalize="none"
          spellCheck={false}
          maxLength={50}
          aria-invalid={invalid}
          aria-describedby="username-hint"
        />
        <p
          id="username-hint"
          className={cn('text-xs', invalid ? 'text-destructive' : 'text-muted-foreground')}
        >
          {hint}
        </p>
      </div>

      <div className="space-y-3">
        <Label>Your club</Label>

        {loadingTeams ? (
          <div className="grid grid-cols-4 gap-2 sm:grid-cols-5">
            {Array.from({ length: 10 }, (_, index) => (
              <Skeleton key={index} className="h-[76px] rounded-xl" />
            ))}
          </div>
        ) : teams && teams.length > 0 ? (
          <ul className="grid grid-cols-4 gap-2 sm:grid-cols-5">
            {teams.map((team) => {
              const selected = team.id === teamId;

              return (
                <li key={team.id}>
                  <button
                    type="button"
                    // Selecting the club you already picked clears it, so
                    // "no club" stays reachable without a separate control.
                    onClick={() => setTeamId(selected ? null : team.id)}
                    aria-pressed={selected}
                    title={team.teamName}
                    className={cn(
                      'relative flex w-full flex-col items-center gap-1.5 rounded-xl border p-2 transition-colors',
                      selected
                        ? 'border-primary/60 bg-primary/10'
                        : 'hover:bg-accent/60 border-transparent',
                    )}
                  >
                    {selected && (
                      <span className="bg-primary text-primary-foreground absolute top-1 right-1 grid size-4 place-items-center rounded-full">
                        <Check className="size-2.5" strokeWidth={3} />
                      </span>
                    )}
                    <TeamBadge teamName={team.teamName} size={40} />
                    <span className="text-muted-foreground w-full truncate text-center text-[10px]">
                      {team.teamName}
                    </span>
                  </button>
                </li>
              );
            })}
          </ul>
        ) : (
          // No season has been seeded yet, so there is nothing to pick from.
          // Said plainly rather than showing an empty grid that reads as broken.
          <p className="text-muted-foreground text-sm">
            No clubs yet — the season has not been set up. You can carry on and
            pick one later.
          </p>
        )}
      </div>

      <div className="space-y-3">
        <Button
          className="w-full rounded-full"
          disabled={invalid || isPending}
          onClick={() => mutate({ username: trimmed, favouriteTeamId: teamId })}
        >
          {isPending && <Loader2 className="size-4 animate-spin" />}
          Start trading
        </Button>

        {error && !conflict && (
          <p className="text-destructive text-center text-xs">
            That did not save. Check your connection and try again.
          </p>
        )}
      </div>
    </div>
  );
};
