import { AlertCircle } from 'lucide-react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Skeleton } from '@/components/ui/skeleton';
import { CreateTeamForm } from '../teams/create-team/CreateTeamForm';
import { useTeamsList } from '../teams/teams-list/useTeamsList';
import { TeamBadge } from '../trades/TeamBadge';
import { SeedSeasonCard } from '../admin/SeedSeasonCard';

export const AdminPage = () => {
  const { data: teams, isLoading, isError, error } = useTeamsList();

  if (isError) {
    return (
      <Card className="border-destructive/40 bg-destructive/5 flex-row items-center gap-3 p-5">
        <AlertCircle className="text-destructive size-5 shrink-0" />
        <p className="text-sm">Error: {error?.message}</p>
      </Card>
    );
  }

  return (
    <div className="space-y-6">
      <header className="space-y-2">
        <h1 className="text-3xl font-semibold tracking-tight sm:text-4xl">Admin</h1>
        <p className="text-muted-foreground max-w-xl text-sm">
          Season setup. The clubs here are the ones players can trade.
        </p>
      </header>

      <SeedSeasonCard />

      <div className="grid gap-6 lg:grid-cols-[22rem_1fr]">
        <Card className="h-fit">
          <CardHeader>
            <CardTitle className="text-base">Add a club</CardTitle>
            <CardDescription>Names must match the club as it appears in results.</CardDescription>
          </CardHeader>
          <CardContent>
            <CreateTeamForm />
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className="text-base">Clubs</CardTitle>
            <CardDescription>
              {isLoading ? 'Loading…' : `${teams?.length ?? 0} in the season`}
            </CardDescription>
          </CardHeader>
          <CardContent>
            {isLoading ? (
              <div className="grid gap-3 sm:grid-cols-2">
                {Array.from({ length: 6 }, (_, index) => (
                  <Skeleton key={index} className="h-14 rounded-lg" />
                ))}
              </div>
            ) : (
              <ul className="grid gap-2 sm:grid-cols-2">
                {/* Defensive check so it doesn't crash if data isn't an array yet */}
                {Array.isArray(teams) &&
                  teams.map((team) => (
                    <li
                      key={team.id}
                      className="border-border/60 bg-background/40 hover:bg-accent/40 flex items-center gap-3 rounded-lg border px-3 py-2.5 transition-colors"
                    >
                      <TeamBadge teamName={team.teamName} size={30} />
                      <span className="truncate text-sm font-medium">{team.teamName}</span>
                    </li>
                  ))}
              </ul>
            )}
          </CardContent>
        </Card>
      </div>
    </div>
  );
};

export default AdminPage;
