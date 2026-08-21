import { useMemo, useState } from 'react';
import { useAuth } from '@workos-inc/authkit-react';
import { AlertCircle, CheckCircle2, Sparkles, TrendingDown, TrendingUp } from 'lucide-react';
import { cn } from '@/lib/utils';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card } from '@/components/ui/card';
import { Checkbox } from '@/components/ui/checkbox';
import { Skeleton } from '@/components/ui/skeleton';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { useTeamsList } from '../teams/teams-list/useTeamsList';
import { useCurrentUser } from '../auth/useCurrentUser';
import { useSubmitTrades } from './useSubmitTrades';
import { useTodaysPrices } from '../prices/useTodaysPrices';
import { coloursFor } from './clubColours';
import { TeamBadge } from './TeamBadge';
import { TRADING_RULES, totalStaked } from './tradingRules';
import { useJokerAllowance } from './useJokerAllowance';

/**
 * 0 to 40 in fives. A fixed list rather than a free number box, so an invalid
 * stake cannot be typed in the first place.
 */
const AMOUNTS = [0, 5, 10, 15, 20, 25, 30, 35, 40];

const MAX_POSITIONS = TRADING_RULES.maxPositions;

type Direction = 'long' | 'short';
type Position = { amount: number; direction: Direction };

const EMPTY: Position = { amount: 0, direction: 'long' };

export const TradeBoard = () => {
  const {
    data: teams,
    isLoading: loadingTeams,
    isError: teamsFailed,
    error: teamsError,
  } = useTeamsList();
  const { user, isLoading: loadingAuth, signIn, signOut } = useAuth();
  const { data: me } = useCurrentUser();
  const { data: prices } = useTodaysPrices();
  const { data: jokerAllowance } = useJokerAllowance();
  const submit = useSubmitTrades();

  const [positions, setPositions] = useState<Record<string, Position>>({});
  const [playJoker, setPlayJoker] = useState(false);

  const active = useMemo(
    () => Object.entries(positions).filter(([, position]) => position.amount > 0),
    [positions],
  );

  const atLimit = active.length >= MAX_POSITIONS;

  const staked = totalStaked(active.map(([, position]) => position.amount));
  const remaining = TRADING_RULES.totalStake - staked;

  // Exactly forty, not at most forty. A player is placing their whole stake and
  // the only question is where, so an incomplete board is not submittable.
  const stakeIsComplete = remaining === 0;
  const canSubmit = Boolean(user) && stakeIsComplete && !submit.isPending;

  // Undefined while the allowance is still loading, which reads as "not spent"
  // — better to briefly offer a joker the API then refuses than to grey out one
  // the player actually has.
  const jokerSpent = jokerAllowance?.available === false;
  const jokerBlockedOn = jokerAllowance?.blockedByUtc?.slice(0, 10) ?? '';

  const update = (teamName: string, change: Partial<Position>) =>
    setPositions((current) => ({
      ...current,
      [teamName]: { ...EMPTY, ...current[teamName], ...change },
    }));

  const onSubmit = () => {
    const exposuresByTeam = Object.fromEntries(
      active.map(([teamName, { amount, direction }]) => [
        teamName,
        direction === 'short' ? -amount : amount,
      ]),
    );

    submit.mutate({ exposuresByTeam, tradeType: playJoker && !jokerSpent ? 'Joker' : 'Standard' });
  };

  if (teamsFailed) {
    return (
      <Card className="border-destructive/40 bg-destructive/5 flex-row items-center gap-3 p-5">
        <AlertCircle className="text-destructive size-5 shrink-0" />
        <p className="text-sm">Could not load teams: {teamsError?.message}</p>
      </Card>
    );
  }

  return (
    <div className="pb-32">
      <header className="mb-8 flex flex-wrap items-end justify-between gap-4">
        <div className="space-y-2">
          <Badge variant="secondary" className="rounded-full px-3 py-1 font-medium">
            Matchweek open
          </Badge>
          <h1 className="text-3xl font-semibold tracking-tight sm:text-4xl">
            This week&rsquo;s trades
          </h1>
          <p className="text-muted-foreground max-w-xl text-sm">
            Back up to {MAX_POSITIONS} clubs, long or short. Your stakes must total exactly{' '}
            {TRADING_RULES.totalStake}, in fives.
          </p>
        </div>

        {/* Identity is read, not chosen. Who you are is settled by the token
            the API validates, so there is nothing here to pick. */}
        <div className="flex items-center gap-3 text-sm">
          {loadingAuth ? (
            <Skeleton className="h-9 w-44 rounded-full" />
          ) : user ? (
            <div className="glass border-border/60 flex items-center gap-3 rounded-full border py-1.5 pr-1.5 pl-4">
              <span className="text-muted-foreground">
                Trading as{' '}
                <span className="text-foreground font-semibold">
                  {me ? `${me.firstName} ${me.lastName}` : user.email}
                </span>
              </span>
              <Button variant="ghost" size="sm" className="rounded-full" onClick={() => signOut()}>
                Sign out
              </Button>
            </div>
          ) : (
            <Button onClick={() => signIn()} className="rounded-full">
              Sign in to trade
            </Button>
          )}
        </div>
      </header>

      {loadingTeams ? (
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
          {Array.from({ length: 8 }, (_, index) => (
            <Skeleton key={index} className="h-[168px] rounded-xl" />
          ))}
        </div>
      ) : (
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
          {teams?.map((team) => {
            const position = positions[team.teamName] ?? EMPTY;
            const isActive = position.amount > 0;
            const isShort = position.direction === 'short';

            // Locked, not hidden: you can still see the club you cannot back,
            // which makes the two-pick limit obvious rather than mysterious.
            const locked = !isActive && atLimit;
            const { primary } = coloursFor(team.teamName);

            // The mid is what a player trades against. Bid and ask sit beside
            // it in smaller type so the spread is visible without competing
            // with the number that actually matters.
            const quote = prices?.get(team.id);

            return (
              <Card
                key={team.id}
                className={cn(
                  'group relative gap-0 overflow-hidden p-0 transition-all duration-200',
                  isActive
                    ? 'shadow-lg ring-1 ring-inset'
                    : 'hover:border-border hover:-translate-y-0.5 hover:shadow-md',
                  locked && 'pointer-events-none opacity-45 saturate-50',
                )}
                style={
                  isActive
                    ? ({
                        '--tw-ring-color': primary,
                        borderColor: 'transparent',
                        background: `linear-gradient(160deg, color-mix(in oklab, ${primary} 14%, var(--card)) 0%, var(--card) 55%)`,
                      } as React.CSSProperties)
                    : undefined
                }
              >
                {/* A club-coloured edge, so a picked card is identifiable from
                    across the grid without reading it. */}
                <span
                  aria-hidden
                  className={cn(
                    'absolute inset-x-0 top-0 h-1 transition-opacity',
                    isActive ? 'opacity-100' : 'opacity-0 group-hover:opacity-60',
                  )}
                  style={{ background: primary }}
                />

                <div className="flex items-center gap-3 p-4 pb-3">
                  <TeamBadge teamName={team.teamName} size={44} />
                  <div className="min-w-0 flex-1">
                    <p className="truncate font-semibold tracking-tight">{team.teamName}</p>
                    {quote ? (
                      <p className="flex items-baseline gap-1.5">
                        <span className="numeric text-lg font-bold">{quote.mid}</span>
                        <span className="text-muted-foreground numeric text-[11px]">
                          {quote.bid} / {quote.ask}
                        </span>
                      </p>
                    ) : (
                      <p className="text-muted-foreground text-xs italic">no price yet</p>
                    )}
                  </div>

                  {isActive && (
                    <Badge
                      className={cn(
                        'numeric shrink-0 gap-1 rounded-full border-0 px-2.5 py-1 text-[11px] font-bold',
                        isShort ? 'bg-short-muted text-short' : 'bg-long-muted text-long',
                      )}
                    >
                      {isShort ? (
                        <TrendingDown className="size-3" />
                      ) : (
                        <TrendingUp className="size-3" />
                      )}
                      {isShort ? '-' : '+'}
                      {position.amount}
                    </Badge>
                  )}
                </div>

                <div className="flex gap-2 p-4 pt-1">
                  <Select
                    value={position.direction}
                    disabled={locked}
                    onValueChange={(value) =>
                      update(team.teamName, { direction: value as Direction })
                    }
                  >
                    <SelectTrigger
                      aria-label={`${team.teamName} direction`}
                      className="bg-background/50 h-9 w-1/2"
                    >
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="long">
                        <TrendingUp className="text-long size-3.5" />
                        Long
                      </SelectItem>
                      <SelectItem value="short">
                        <TrendingDown className="text-short size-3.5" />
                        Short
                      </SelectItem>
                    </SelectContent>
                  </Select>

                  <Select
                    value={String(position.amount)}
                    disabled={locked}
                    onValueChange={(value) => update(team.teamName, { amount: Number(value) })}
                  >
                    <SelectTrigger
                      aria-label={`${team.teamName} amount`}
                      className="bg-background/50 numeric h-9 w-1/2"
                    >
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      {AMOUNTS.map((amount) => (
                        <SelectItem key={amount} value={String(amount)} className="numeric">
                          {amount === 0 ? '—' : amount}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
              </Card>
            );
          })}
        </div>
      )}

      <div className="fixed inset-x-0 bottom-0 z-30">
        <div className="glass border-border/60 border-t">
          <div className="mx-auto flex w-full max-w-7xl flex-wrap items-center justify-between gap-3 px-4 py-3 sm:px-6 lg:px-8">
            <div className="flex flex-wrap items-center gap-x-5 gap-y-2 text-sm">
              <div className="flex items-center gap-3">
                {/* The stake meter. A number alone makes you do the arithmetic;
                    the bar shows how much of the forty is placed at a glance. */}
                <div className="bg-muted h-1.5 w-24 overflow-hidden rounded-full">
                  <div
                    className={cn(
                      'h-full rounded-full transition-all duration-300',
                      remaining < 0 ? 'bg-short' : stakeIsComplete ? 'bg-long' : 'bg-primary',
                    )}
                    style={{
                      width: `${Math.min(100, (staked / TRADING_RULES.totalStake) * 100)}%`,
                    }}
                  />
                </div>
                <div>
                  <span
                    className={cn(
                      'numeric text-base font-bold',
                      stakeIsComplete && 'text-long',
                      remaining < 0 && 'text-short',
                    )}
                  >
                    {staked} / {TRADING_RULES.totalStake}
                  </span>{' '}
                  <span className="text-muted-foreground text-xs">
                    {stakeIsComplete
                      ? 'staked'
                      : remaining > 0
                        ? `staked — ${remaining} left to place`
                        : `staked — ${Math.abs(remaining)} over`}
                  </span>
                </div>
              </div>

              {active.length > 0 && (
                <div className="flex flex-wrap items-center gap-1.5">
                  {active.map(([team, p]) => (
                    <Badge
                      key={team}
                      variant="outline"
                      className={cn(
                        'numeric rounded-full text-[11px]',
                        p.direction === 'short'
                          ? 'border-short/40 text-short'
                          : 'border-long/40 text-long',
                      )}
                    >
                      {team} {p.direction === 'short' ? '-' : '+'}
                      {p.amount}
                    </Badge>
                  ))}
                </div>
              )}

              {/* Persisted per trade and read back by the PnL multiplier, so this
                  is a real decision rather than a display toggle. Disabled from
                  the same rule the API enforces, so nobody decides to play a
                  joker and only then gets refused. */}
              <label
                className={cn(
                  'flex items-center gap-2 rounded-full border px-3 py-1.5 transition-colors',
                  jokerSpent
                    ? 'border-border bg-muted/50 cursor-not-allowed'
                    : 'border-joker/40 bg-joker/10 hover:bg-joker/15 cursor-pointer',
                )}
                title={
                  jokerSpent
                    ? `Joker already played on ${jokerBlockedOn} - one per calendar year, per season.`
                    : 'Doubles this week’s points'
                }
              >
                <Checkbox
                  checked={playJoker && !jokerSpent}
                  disabled={jokerSpent}
                  onCheckedChange={(checked) => setPlayJoker(checked === true)}
                  className="data-[state=checked]:bg-joker data-[state=checked]:border-joker data-[state=checked]:text-joker-foreground size-4"
                />
                <Sparkles
                  className={cn('size-3.5', jokerSpent ? 'text-muted-foreground' : 'text-joker')}
                />
                <span
                  className={cn(
                    'text-xs font-semibold',
                    jokerSpent ? 'text-muted-foreground' : 'text-foreground',
                  )}
                >
                  Play joker
                </span>
                <span
                  className={cn('text-[11px]', jokerSpent ? 'text-muted-foreground' : 'text-joker')}
                >
                  {jokerSpent ? `used ${jokerBlockedOn}` : '2× points'}
                </span>
              </label>
            </div>

            <div className="flex items-center gap-3">
              {submit.isSuccess && (
                <span className="text-long flex items-center gap-1.5 text-sm font-medium">
                  <CheckCircle2 className="size-4" />
                  Trades submitted.
                </span>
              )}
              {submit.isError && (
                <span className="text-short flex items-center gap-1.5 text-sm font-medium">
                  <AlertCircle className="size-4" />
                  {(submit.error as { response?: { data?: { detail?: string } } })?.response?.data
                    ?.detail ?? 'Could not submit. Try again.'}
                </span>
              )}
              <Button
                size="lg"
                className="rounded-full px-6 font-semibold shadow-lg"
                disabled={!canSubmit}
                onClick={onSubmit}
              >
                {submit.isPending ? 'Submitting...' : 'Submit trades'}
              </Button>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default TradeBoard;
