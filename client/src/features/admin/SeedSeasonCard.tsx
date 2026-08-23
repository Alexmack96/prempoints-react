import { useState } from 'react';
import { CalendarDays, CheckCircle2, Loader2, Lock } from 'lucide-react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { useSeasonCovering, useSeedNewSeason } from './seedNewSeason';

/**
 * Stands up a whole season: the season row, its weekly gameweeks, the clubs and
 * the enrolments that say who is playing.
 *
 * Locked once a season already covers the start date. The API refuses a second
 * season for the same start year anyway — that is the real guarantee — but a
 * button that fails after you have typed twenty club names is a worse way to
 * find out than a form that will not offer itself.
 */
export const SeedSeasonCard = () => {
  const [seasonName, setSeasonName] = useState('2026/27');
  const [startDate, setStartDate] = useState('2026-08-14');
  const [endDate, setEndDate] = useState('2027-05-23');
  const [roster, setRoster] = useState('');

  const { data: existing, isLoading: checking } = useSeasonCovering(startDate || null);
  const { mutate, isPending, data: result, error } = useSeedNewSeason();

  const teams = roster
    .split('\n')
    .map((line) => line.trim())
    .filter((line) => line.length > 0);

  const alreadySeeded = Boolean(existing);
  const datesInOrder = startDate < endDate;
  const canSubmit =
    !alreadySeeded && !checking && !isPending && teams.length > 0 && datesInOrder && seasonName.length > 0;

  if (result) {
    return (
      <Card className="border-primary/40 bg-primary/5 h-fit">
        <CardHeader>
          <CardTitle className="flex items-center gap-2 text-base">
            <CheckCircle2 className="text-primary size-4" />
            {result.seasonName} is set up
          </CardTitle>
          <CardDescription>
            {result.gameweeksCreated} gameweeks, {result.teamsEnrolled.length} clubs enrolled,{' '}
            {result.teamsCreated.length} newly created.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <p className="text-muted-foreground text-xs">
            Load prices next, then activate players for the season.
          </p>
        </CardContent>
      </Card>
    );
  }

  return (
    <Card className="h-fit">
      <CardHeader>
        <CardTitle className="flex items-center gap-2 text-base">
          <CalendarDays className="size-4 opacity-70" />
          Seed a season
        </CardTitle>
        <CardDescription>
          Creates the season, its weekly gameweeks, and every club in one go. Run once a year.
        </CardDescription>
      </CardHeader>

      <CardContent className="space-y-4">
        {alreadySeeded && (
          <div className="border-border/60 bg-muted/40 flex items-start gap-2.5 rounded-lg border p-3">
            <Lock className="mt-0.5 size-4 shrink-0 opacity-70" />
            <p className="text-xs">
              <span className="font-medium">{existing?.seasonName}</span> already covers{' '}
              {startDate}. Seeding is locked so a second run cannot duplicate it. Change the start
              date to set up a different season.
            </p>
          </div>
        )}

        <div className="space-y-2">
          <Label htmlFor="seasonName">Season name</Label>
          <Input
            id="seasonName"
            value={seasonName}
            onChange={(event) => setSeasonName(event.target.value)}
            disabled={alreadySeeded}
          />
        </div>

        <div className="grid gap-3 sm:grid-cols-2">
          <div className="space-y-2">
            <Label htmlFor="startDate">First day</Label>
            <Input
              id="startDate"
              type="date"
              value={startDate}
              onChange={(event) => setStartDate(event.target.value)}
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="endDate">Last day</Label>
            <Input
              id="endDate"
              type="date"
              value={endDate}
              onChange={(event) => setEndDate(event.target.value)}
              disabled={alreadySeeded}
              aria-invalid={!datesInOrder}
            />
          </div>
        </div>

        {!datesInOrder && (
          <p className="text-destructive text-xs">The season has to end after it starts.</p>
        )}

        <div className="space-y-2">
          <Label htmlFor="roster">Clubs, one per line</Label>
          <textarea
            id="roster"
            rows={8}
            value={roster}
            onChange={(event) => setRoster(event.target.value)}
            disabled={alreadySeeded}
            placeholder={'Arsenal\nAston Villa\nBournemouth\n…'}
            spellCheck={false}
            className="border-input focus-visible:border-ring focus-visible:ring-ring/50 dark:bg-input/30 w-full rounded-md border bg-transparent px-3 py-2 text-sm shadow-xs outline-none focus-visible:ring-[3px] disabled:cursor-not-allowed disabled:opacity-50"
          />
          <p className="text-muted-foreground text-xs">
            {teams.length} club{teams.length === 1 ? '' : 's'}. Spell them the way the badge files
            are named, or the crest falls back to initials.
          </p>
        </div>

        <Button
          className="w-full rounded-full"
          disabled={!canSubmit}
          onClick={() =>
            mutate({
              seasonName,
              startDate,
              endDate,
              // The first season has nothing to carry forward, so the whole
              // league goes in as promoted. Later years inherit last season's
              // roster and only need the three that came up.
              promotedTeams: teams,
              relegatedTeams: [],
            })
          }
        >
          {isPending && <Loader2 className="size-4 animate-spin" />}
          {alreadySeeded ? 'Already seeded' : `Seed ${seasonName}`}
        </Button>

        {error !== null && (
          <p className="text-destructive text-xs">
            {/* The API answers 409 when the start year is taken, or when gameweek
                dates overlap an existing season. Its message says which. */}
            {(error as { response?: { data?: { detail?: string } } }).response?.data?.detail ??
              'That did not work. Check the dates and try again.'}
          </p>
        )}
      </CardContent>
    </Card>
  );
};
