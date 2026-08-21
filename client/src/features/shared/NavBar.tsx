import { useEffect, useState } from 'react';
import { NavLink, useNavigate } from 'react-router-dom';
import { BarChart3, LineChart, LogOut, Menu, Settings, Shield, Trophy, X } from 'lucide-react';
import { cn } from '@/lib/utils';
import { Button } from '@/components/ui/button';
import { ThemeToggle } from '@/components/theme/ThemeToggle';
import { useCurrentUser } from '../auth/useCurrentUser';
import image_prem_lion from '../../assets/prem_lion.jpg';

/**
 * The nav and the keyboard shortcuts read from one list, so a route can never
 * be reachable by Ctrl+Alt+n and missing from the bar, or the other way round.
 *
 * `adminOnly` hides an item from players. Hiding is courtesy, not protection —
 * the route guards itself and the API enforces the role — but a link nobody can
 * use has no business being in the bar.
 */
const NAV_ITEMS = [
  { to: '/', label: 'Trade', icon: LineChart, shortcut: '1', end: true, adminOnly: false },
  {
    to: '/leaderboard',
    label: 'Leaderboard',
    icon: Trophy,
    shortcut: '2',
    end: false,
    adminOnly: false,
  },
  { to: '/prices', label: 'Prices', icon: BarChart3, shortcut: '3', end: false, adminOnly: false },
  { to: '/results', label: 'Results', icon: Shield, shortcut: '4', end: false, adminOnly: false },
  { to: '/admin', label: 'Admin', icon: Settings, shortcut: '5', end: false, adminOnly: true },
] as const;

const SHORTCUTS: Record<string, string> = {
  ...Object.fromEntries(NAV_ITEMS.map((item) => [item.shortcut, item.to])),
  '0': '/logout',
};

export default function NavBar() {
  const navigate = useNavigate();
  const [menuOpen, setMenuOpen] = useState(false);
  const { data: me } = useCurrentUser();

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
              <span className="text-muted-foreground block text-[11px] leading-tight">2025/26</span>
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
            <Button
              variant="ghost"
              size="icon"
              className="md:hidden"
              aria-label={menuOpen ? 'Close menu' : 'Open menu'}
              aria-expanded={menuOpen}
              onClick={() => setMenuOpen((open) => !open)}
            >
              {menuOpen ? <X className="size-4" /> : <Menu className="size-4" />}
            </Button>
          </div>
        </div>

        {menuOpen && (
          <nav className="border-border/60 border-t px-4 py-3 md:hidden">
            <ul className="flex flex-col gap-1">
              {items.map(({ to, label, icon: Icon, end }) => (
                <li key={to}>
                  <NavLink
                    to={to}
                    end={end}
                    className={linkClasses}
                    onClick={() => setMenuOpen(false)}
                  >
                    <Icon className="size-4 opacity-70" />
                    {label}
                  </NavLink>
                </li>
              ))}
              <li>
                <NavLink to="/logout" className={linkClasses} onClick={() => setMenuOpen(false)}>
                  <LogOut className="size-4 opacity-70" />
                  Log out
                </NavLink>
              </li>
            </ul>
          </nav>
        )}
      </div>
    </header>
  );
}
