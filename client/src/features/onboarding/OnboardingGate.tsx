import { useState } from 'react';
import { Loader2 } from 'lucide-react';
import { cn } from '@/lib/utils';
import { Button } from '@/components/ui/button';
import { useCurrentUser } from '../auth/useCurrentUser';
import { useTeamsList } from '../teams/teams-list/useTeamsList';
import { useUpdateMyProfile } from '../users/useUpdateMyProfile';
import { TeamBadge } from '../trades/TeamBadge';

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

  const tooShort = username.trim().length < 3;
  const badCharacters = username.length > 0 && !USERNAME_PATTERN.test(username);
  const canSubmit = !tooShort && !badCharacters && !isPending;

  // The API answers 409 with a ProblemDetails when the name is taken. Shown
  // against the field, because that is the one thing the player can act on.
  const conflict =
    error !== null &&
    typeof error === 'object' &&
    'response' in error &&
    (error as { response?: { status?: number } }).response?.status === 409;

  return (
    <div className="mx-auto flex min-h-dvh w-full max-w-lg flex-col justify-center gap-8 px-4 py-10">
      <header className="space-y-2">
        <h1 className="text-2xl font-semibold tracking-tight">Welcome to PremPoints</h1>
        <p className="text-muted-foreground text-sm">
          Two quick things and you are in. Both can be changed later.
        </p>
      </header>

      <div className="space-y-2">
        <label htmlFor="username" className="text-sm font-medium">
          Your name on the leaderboard
        </label>
        <input
          id="username"
          value={username}
          onChange={(event) => setUsername(event.target.value)}
          autoComplete="off"
          autoCapitalize="none"
          spellCheck={false}
          maxLength={50}
          className="border-border/60 bg-background focus-visible:ring-primary/40 w-full rounded-xl border px-3.5 py-2.5 text-sm outline-none focus-visible:ring-2"
        />
        <p
          className={cn(
            'text-xs',
            badCharacters || conflict ? 'text-destructive' : 'text-muted-foreground',
          )}
        >
          {conflict
            ? `${username} is already taken.`
            : badCharacters
              ? 'Letters, digits, hyphens and underscores only.'
              : tooShort
                ? 'At least three characters.'
                : `We suggested ${suggestedUsername} — keep it or change it.`}
        </p>
      </div>

      <div className="space-y-3">
        <span className="text-sm font-medium">Your club</span>

        {loadingTeams ? (
          <p className="text-muted-foreground text-sm">Loading clubs…</p>
        ) : (
          <ul className="grid grid-cols-4 gap-2 sm:grid-cols-5">
            {teams?.map((team) => {
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
                      'flex w-full flex-col items-center gap-1.5 rounded-xl border p-2 transition-colors',
                      selected
                        ? 'border-primary/60 bg-primary/10'
                        : 'border-transparent hover:bg-accent/60',
                    )}
                  >
                    <TeamBadge teamName={team.teamName} size={40} />
                    <span className="text-muted-foreground w-full truncate text-center text-[10px]">
                      {team.teamName}
                    </span>
                  </button>
                </li>
              );
            })}
          </ul>
        )}
      </div>

      <Button
        className="rounded-full"
        disabled={!canSubmit}
        onClick={() => mutate({ username: username.trim(), favouriteTeamId: teamId })}
      >
        {isPending && <Loader2 className="size-4 animate-spin" />}
        {teamId ? 'Start trading' : 'Skip the club, start trading'}
      </Button>

      {error && !conflict && (
        <p className="text-destructive text-xs">
          That did not save. Check your connection and try again.
        </p>
      )}
    </div>
  );
};
