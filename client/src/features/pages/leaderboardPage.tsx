import { AlertCircle, Trophy } from 'lucide-react';
import { useAuth } from '@workos-inc/authkit-react';
import { cn } from '@/lib/utils';
import { Badge } from '@/components/ui/badge';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Skeleton } from '@/components/ui/skeleton';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';
import { useLeaderboard } from '../leaderboard/useLeaderboard';
import { useCurrentUser } from '../auth/useCurrentUser';

/**
 * Initials for the avatar disc. Two letters from the name, so the column has
 * something to anchor on before anyone has a score to look at.
 */
const initials = (firstName: string, lastName: string) =>
  `${firstName.charAt(0)}${lastName.charAt(0)}`.toUpperCase();

const Pnl = ({ value, settled }: { value: number; settled: boolean }) => {
  if (!settled) {
    return <span className="text-muted-foreground numeric text-sm">&mdash;</span>;
  }

  return (
    <span
      className={cn(
        'numeric font-bold',
        value > 0 && 'text-long',
        value < 0 && 'text-short',
        value === 0 && 'text-muted-foreground',
      )}
    >
      {value > 0 ? '+' : ''}
      {value}
    </span>
  );
};

export const LeaderboardPage = () => {
  const { data: rows, isLoading, isError, error } = useLeaderboard();
  const { user } = useAuth();
  const { data: me } = useCurrentUser();

  if (isError) {
    return (
      <Card className="border-destructive/40 bg-destructive/5 flex-row items-center gap-3 p-5">
        <AlertCircle className="text-destructive size-5 shrink-0" />
        <p className="text-sm">Could not load the leaderboard: {error?.message}</p>
      </Card>
    );
  }

  // One flag for the whole board: settlement is a property of the season, not
  // of a player, so every row reports the same thing.
  const scored = rows?.some((row) => row.pnlIsSettled) ?? false;

  return (
    <div className="space-y-6">
      <header className="space-y-2">
        <h1 className="text-3xl font-semibold tracking-tight sm:text-4xl">Leaderboard</h1>
        <p className="text-muted-foreground max-w-xl text-sm">
          {scored
            ? 'Every player in the season, best first.'
            : 'Every player in the season. Nothing has settled yet, so everyone is level.'}
        </p>
      </header>

      <Card className="overflow-hidden py-0">
        <CardHeader className="border-border/60 gap-1 border-b py-4">
          <CardTitle className="text-base">2025/26 standings</CardTitle>
          <CardDescription>
            {isLoading ? 'Loading…' : `${rows?.length ?? 0} players enrolled`}
          </CardDescription>
        </CardHeader>
        <CardContent className="px-0">
          {isLoading ? (
            <div className="space-y-3 p-4">
              {Array.from({ length: 6 }, (_, index) => (
                <Skeleton key={index} className="h-10 w-full" />
              ))}
            </div>
          ) : rows?.length === 0 ? (
            <div className="flex flex-col items-center gap-3 py-16 text-center">
              <div className="bg-muted text-muted-foreground rounded-full p-4">
                <Trophy className="size-6" />
              </div>
              <p className="font-medium">Nobody has joined the season yet</p>
              <p className="text-muted-foreground max-w-sm text-sm">
                Players appear here as soon as they are enrolled, score or no score.
              </p>
            </div>
          ) : (
            <div className="overflow-x-auto">
              <Table className="min-w-[34rem]">
                <TableHeader>
                  <TableRow className="hover:bg-transparent">
                    <TableHead className="w-16 px-5 text-xs tracking-wide uppercase">#</TableHead>
                    <TableHead className="px-5 text-xs tracking-wide uppercase">Player</TableHead>
                    <TableHead className="px-5 text-right text-xs tracking-wide uppercase">
                      Trades
                    </TableHead>
                    <TableHead className="px-5 text-right text-xs tracking-wide uppercase">
                      P&amp;L
                    </TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {rows?.map((row) => {
                    // Highlighting your own row is the one thing a standings
                    // table is always asked for, and finding yourself in a list
                    // of twenty names is otherwise a scan every visit.
                    const isMe = Boolean(user) && me?.id === row.userId;

                    return (
                      <TableRow key={row.userId} className={cn(isMe && 'bg-primary/6')}>
                        <TableCell className="numeric px-5 py-3 font-bold">
                          {row.rank === 1 && scored ? (
                            <span className="text-joker inline-flex items-center gap-1">
                              <Trophy className="size-3.5" />1
                            </span>
                          ) : (
                            row.rank
                          )}
                        </TableCell>
                        <TableCell className="px-5 py-3">
                          <div className="flex items-center gap-3">
                            <span className="bg-primary/15 text-primary numeric flex size-8 shrink-0 items-center justify-center rounded-full text-xs font-bold">
                              {initials(row.firstName, row.lastName)}
                            </span>
                            <div className="min-w-0">
                              <p className="truncate text-sm font-medium">
                                {row.firstName} {row.lastName}
                              </p>
                              <p className="text-muted-foreground truncate text-xs">
                                {row.username}
                              </p>
                            </div>
                            {isMe && (
                              <Badge variant="secondary" className="rounded-full text-[10px]">
                                You
                              </Badge>
                            )}
                          </div>
                        </TableCell>
                        <TableCell className="text-muted-foreground numeric px-5 py-3 text-right text-sm">
                          {row.tradesPlaced}
                        </TableCell>
                        <TableCell className="px-5 py-3 text-right">
                          <Pnl value={row.pnl} settled={row.pnlIsSettled} />
                        </TableCell>
                      </TableRow>
                    );
                  })}
                </TableBody>
              </Table>
            </div>
          )}
        </CardContent>
      </Card>

      {!isLoading && !scored && rows && rows.length > 0 && (
        <p className="text-muted-foreground text-xs">
          P&amp;L shows a dash until trades are marked against a settled price. Trades placed are
          counted from the moment they are submitted.
        </p>
      )}
    </div>
  );
};

export default LeaderboardPage;
