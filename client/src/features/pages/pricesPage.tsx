import { AlertCircle, Minus, TrendingDown, TrendingUp } from 'lucide-react';
import { cn } from '@/lib/utils';
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
import { usePriceSummary } from '../prices/usePriceSummary';
import type { PriceMovement } from '../prices/priceSummaryDto';
import { TeamBadge } from '../trades/TeamBadge';

/**
 * Which way the price moved at the last cut.
 *
 * Placeholder-shaped on purpose: this is where the form indicator lands once
 * results feed in, and it already has the right inputs — the latest mid and the
 * one before it.
 */
const Movement = ({ movement }: { movement: PriceMovement }) => {
  if (movement === 'Up') {
    return (
      <span className="bg-long-muted text-long inline-flex size-6 items-center justify-center rounded-full">
        <TrendingUp className="size-3.5" />
      </span>
    );
  }

  if (movement === 'Down') {
    return (
      <span className="bg-short-muted text-short inline-flex size-6 items-center justify-center rounded-full">
        <TrendingDown className="size-3.5" />
      </span>
    );
  }

  if (movement === 'Level') {
    return (
      <span className="bg-muted text-muted-foreground inline-flex size-6 items-center justify-center rounded-full">
        <Minus className="size-3.5" />
      </span>
    );
  }

  // Unknown: one price or none, so there is nothing to compare against. Shown
  // as blank rather than "level", which would claim the price held steady.
  return <span className="text-muted-foreground/40">&middot;</span>;
};

export const PricesPage = () => {
  const { data: summary, isLoading, isError, error } = usePriceSummary();

  if (isError) {
    return (
      <Card className="border-destructive/40 bg-destructive/5 flex-row items-center gap-3 p-5">
        <AlertCircle className="text-destructive size-5 shrink-0" />
        <p className="text-sm">Could not load prices: {error?.message}</p>
      </Card>
    );
  }

  return (
    <div className="space-y-6">
      <header className="space-y-2">
        <h1 className="text-3xl font-semibold tracking-tight sm:text-4xl">Prices</h1>
        <p className="text-muted-foreground max-w-xl text-sm">
          Latest quote per club, highest first. The mid is what trades settle against.
        </p>
      </header>

      <Card className="overflow-hidden py-0">
        <CardHeader className="border-border/60 gap-1 border-b py-4">
          <CardTitle className="text-base">Market</CardTitle>
          <CardDescription>{summary?.length ?? 0} clubs quoted</CardDescription>
        </CardHeader>
        <CardContent className="px-0">
          {isLoading ? (
            <div className="space-y-3 p-4">
              {Array.from({ length: 10 }, (_, index) => (
                <Skeleton key={index} className="h-10 w-full" />
              ))}
            </div>
          ) : (
            <div className="overflow-x-auto">
              <Table className="min-w-[36rem]">
                <TableHeader>
                  <TableRow className="hover:bg-transparent">
                    <TableHead className="px-5 text-xs tracking-wide uppercase">Club</TableHead>
                    <TableHead className="px-5 text-right text-xs tracking-wide uppercase">
                      Mid
                    </TableHead>
                    <TableHead className="px-5 text-right text-xs tracking-wide uppercase">
                      Sell / Buy
                    </TableHead>
                    <TableHead className="px-5 text-center text-xs tracking-wide uppercase">
                      Move
                    </TableHead>
                    <TableHead className="px-5 text-right text-xs tracking-wide uppercase">
                      As at
                    </TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {summary?.map((row) => (
                    <TableRow key={row.teamId}>
                      <TableCell className="px-5 py-3">
                        <div className="flex items-center gap-3">
                          <TeamBadge teamName={row.teamName} size={28} />
                          <span className="font-medium">{row.teamName}</span>
                        </div>
                      </TableCell>
                      <TableCell
                        className={cn(
                          'numeric px-5 py-3 text-right text-base font-bold',
                          row.movement === 'Up' && 'text-long',
                          row.movement === 'Down' && 'text-short',
                        )}
                      >
                        {row.mid ?? (
                          <span className="text-muted-foreground text-xs font-normal italic">
                            no price
                          </span>
                        )}
                      </TableCell>
                      <TableCell className="text-muted-foreground numeric px-5 py-3 text-right text-xs">
                        {row.bid !== null && row.ask !== null ? `${row.bid} / ${row.ask}` : ''}
                      </TableCell>
                      <TableCell className="px-5 py-3 text-center">
                        <Movement movement={row.movement} />
                      </TableCell>
                      <TableCell className="text-muted-foreground numeric px-5 py-3 text-right text-xs">
                        {row.valueDate ?? ''}
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
};

export default PricesPage;
