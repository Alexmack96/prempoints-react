import { useEffect } from 'react';
import { Link } from 'react-router-dom';
import { LogOut } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';

export default function Logout() {
  // Logic to run when the page mounts
  useEffect(() => {
    // 1. Clear local storage / session storage
    localStorage.removeItem('token');
    localStorage.removeItem('user');

    // 2. If you are using a global auth state (like Context or Redux),
    // dispatch your logout action here.
    console.log('User has been logged out cleanup complete.');
  }, []);

  return (
    <div className="flex min-h-[70vh] items-center justify-center px-4">
      <Card className="glass w-full max-w-md text-center">
        <CardContent className="flex flex-col items-center gap-6 py-10">
          <div className="from-primary/25 to-primary/5 ring-primary/20 rounded-full bg-gradient-to-br p-5 ring-1">
            <LogOut className="text-primary size-8" />
          </div>

          <div className="space-y-2">
            <h1 className="text-2xl font-semibold tracking-tight">You have been logged out</h1>
            <p className="text-muted-foreground text-sm">
              Thanks for playing PremPoints 2025/26. Your positions are safe until next matchweek.
            </p>
          </div>

          {/* One way back. Signing in happens on the board itself, so a
              separate "sign in" button would land in the same place. */}
          <Button asChild className="w-full rounded-full sm:w-auto sm:px-8">
            <Link to="/">Back to the board</Link>
          </Button>
        </CardContent>
      </Card>
    </div>
  );
}
