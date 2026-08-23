import { useEffect } from 'react';
import { NavLink, useNavigate } from 'react-router-dom';
import { LogOut } from 'lucide-react';
import { cn } from '@/lib/utils';
import { Button } from '@/components/ui/button';
import { ThemeToggle } from '@/components/theme/ThemeToggle';
import { useCurrentUser } from '../auth/useCurrentUser';
import { useSeasonLabel } from '../seasons/useCurrentSeason';
import { NAV_ITEMS, SHORTCUTS } from './navItems';
import BottomNav from './BottomNav';
import image_prem_lion from '../../assets/prem_lion.jpg';

export default function NavBar() {
  const navigate = useNavigate();
  const { data: me } = useCurrentUser();
  const season = useSeasonLabel();

  const isAdmin = me?.role === 'Administrator';
  const items = NAV_ITEMS.filter((item) => !item.adminOnly || isAdmin);

  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (!e.ctrlKey || !e.altKey) return;

      const destination = SHORTCUTS[e.key];

      if (destination) {
        e.preventDefault();
        navigate(destination);
      }
    };

    window.addEventListener('keydown', handleKeyDown);

    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [navigate]);

  const linkClasses = ({ isActive }: { isActive: boolean }) =>
    cn(
      'relative flex items-center gap-2 rounded-full px-3.5 py-2 text-sm font-medium transition-colors',
      isActive
        ? 'bg-primary/12 text-foreground shadow-[inset_0_0_0_1px] shadow-primary/25'
        : 'text-muted-foreground hover:bg-accent/60 hover:text-foreground',
    );

  return (
    <>
      <header className="sticky top-0 z-40">
        <div className="glass border-border/60 border-b">
          <div className="mx-auto flex h-16 w-full max-w-7xl items-center justify-between gap-4 px-4 sm:px-6 lg:px-8">
            <NavLink to="/" className="flex min-w-0 items-center gap-3">
              <span className="from-primary/70 to-primary/20 rounded-full bg-gradient-to-br p-[2px]">
                <img
                  src={image_prem_lion}
                  alt=""
                  className="border-background size-9 rounded-full border-2 object-cover"
                />
              </span>
              <span className="min-w-0">
                <span className="block truncate text-sm leading-tight font-semibold tracking-tight">
                  PremPoints
                </span>
                {season && (
                  <span className="text-muted-foreground block text-[11px] leading-tight">
                    {season}
                  </span>
                )}
              </span>
            </NavLink>

            <nav className="hidden items-center gap-1 md:flex">
              {items.map(({ to, label, icon: Icon, shortcut, end }) => (
                <NavLink
                  key={to}
                  to={to}
                  end={end}
                  className={linkClasses}
                  title={`Ctrl+Alt+${shortcut}`}
                >
                  <Icon className="size-4 opacity-70" />
                  {label}
                </NavLink>
              ))}
            </nav>

            <div className="flex items-center gap-1">
              <ThemeToggle />
              {/* Log out keeps its place in the top bar on every width. It is
                  not a destination, and giving it a sixth tab alongside the
                  five real ones would invite a mis-tap on the way to Admin. */}
              <Button
                asChild
                variant="ghost"
                size="icon"
                className="text-muted-foreground hover:text-foreground md:hidden"
              >
                <NavLink to="/logout" aria-label="Log out">
                  <LogOut className="size-4" />
                </NavLink>
              </Button>
              <Button
                asChild
                variant="ghost"
                size="sm"
                className="text-muted-foreground hover:text-foreground hidden md:inline-flex"
              >
                <NavLink to="/logout" title="Ctrl+Alt+0">
                  <LogOut className="size-4" />
                  Log out
                </NavLink>
              </Button>
            </div>
          </div>
        </div>
      </header>

      <BottomNav items={items} />
    </>
  );
}
