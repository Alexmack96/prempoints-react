import type { ReactNode } from 'react';
import { useAuth } from '@workos-inc/authkit-react';
import { Lock } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Skeleton } from '@/components/ui/skeleton';
import { useCurrentUser } from './useCurrentUser';

/**
 * Gates a route on the Administrator role.
 *
 * The nav already hides what a player cannot use, but hiding a link is not
 * access control — the URL is still typeable. This is the client half of the
 * check; the API enforces the same role on every endpoint behind it, and that
 * is the half that decides.
 */
export const RequireAdmin = ({ children }: { children: ReactNode }) => {
  const { user, isLoading: loadingAuth, signIn } = useAuth();
  const { data: me, isLoading: loadingUser } = useCurrentUser();

  if (loadingAuth || (user && loadingUser)) {
    return (
      <div className="space-y-4">
        <Skeleton className="h-10 w-64" />
        <Skeleton className="h-64 w-full rounded-xl" />
      </div>
    );
  }

  if (!user) {
    return (
      <Denied
        title="Sign in to continue"
        body="This area is for season administrators."
        action={
          <Button className="rounded-full" onClick={() => signIn()}>
            Sign in
          </Button>
        }
      />
    );
  }

  if (me?.role !== 'Administrator') {
    return (
      <Denied
        title="Not your area"
        body="Only season administrators can manage clubs and seasons."
      />
    );
  }

  return children;
};

const Denied = ({ title, body, action }: { title: string; body: string; action?: ReactNode }) => (
  <Card className="mx-auto mt-12 max-w-md">
    <CardContent className="flex flex-col items-center gap-4 py-12 text-center">
      <div className="bg-muted text-muted-foreground rounded-full p-4">
        <Lock className="size-6" />
      </div>
      <div className="space-y-1">
        <p className="font-semibold">{title}</p>
        <p className="text-muted-foreground text-sm">{body}</p>
      </div>
      {action}
    </CardContent>
  </Card>
);
