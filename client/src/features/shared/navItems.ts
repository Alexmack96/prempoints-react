import { BarChart3, LineChart, Settings, Shield, Trophy } from 'lucide-react';
import type { LucideIcon } from 'lucide-react';

export type NavItem = {
  to: string;
  label: string;
  icon: LucideIcon;
  shortcut: string;
  end: boolean;
  adminOnly: boolean;
};

/**
 * The one list every navigation surface reads: the top bar on desktop, the
 * bottom tab bar on mobile, and the Ctrl+Alt shortcuts. Lifted out of NavBar
 * when the bottom bar arrived, because two components each holding their own
 * copy is how a route ends up in one and missing from the other.
 *
 * `adminOnly` hides an item from players. Hiding is courtesy, not protection —
 * the route guards itself and the API enforces the role — but a link nobody can
 * use has no business being in the bar.
 */
export const NAV_ITEMS: readonly NavItem[] = [
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
];

export const SHORTCUTS: Record<string, string> = {
  ...Object.fromEntries(NAV_ITEMS.map((item) => [item.shortcut, item.to])),
  '0': '/logout',
};
