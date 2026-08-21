import type { LucideIcon } from 'lucide-react';
import { Card, CardContent } from '@/components/ui/card';

/**
 * A page that exists in the nav but not yet in the API. Says so plainly rather
 * than showing an empty table, which reads as a bug.
 */
export const ComingSoon = ({
  title,
  description,
  icon: Icon,
}: {
  title: string;
  description: string;
  icon: LucideIcon;
}) => (
  <div className="space-y-6">
    <header className="space-y-2">
      <h1 className="text-3xl font-semibold tracking-tight sm:text-4xl">{title}</h1>
      <p className="text-muted-foreground max-w-xl text-sm">{description}</p>
    </header>

    <Card className="border-dashed">
      <CardContent className="flex flex-col items-center gap-3 py-16 text-center">
        <div className="bg-muted text-muted-foreground rounded-full p-4">
          <Icon className="size-6" />
        </div>
        <p className="font-medium">Nothing to show yet</p>
        <p className="text-muted-foreground max-w-sm text-sm">
          This lands once results start feeding in for the season.
        </p>
      </CardContent>
    </Card>
  </div>
);
