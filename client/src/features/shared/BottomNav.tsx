import { NavLink } from 'react-router-dom';
import { cn } from '@/lib/utils';
import type { NavItem } from './navItems';

/**
 * The mobile navigation: a fixed tab bar at the bottom of the viewport.
 *
 * Bottom rather than top because this is reached one-handed on a phone, and the
 * top of a modern handset is the part of the screen a thumb cannot get to. It
 * replaces a hamburger, which cost two taps and hid every destination behind a
 * control that says nothing about what is inside it.
 *
 * Hidden from `md` up, where NavBar's inline links take over and a screen wide
 * enough to show five labels has no reason to hide them.
 */
export default function BottomNav({ items }: { items: readonly NavItem[] }) {
  return (
    <nav
      aria-label="Primary"
      className={cn(
        'glass border-border/60 fixed inset-x-0 bottom-0 z-40 border-t md:hidden',
        // The home indicator on a notched iPhone sits over the bottom of the
        // viewport. Without this the last few pixels of every tab are under it.
        'pb-[env(safe-area-inset-bottom)]',
      )}
    >
      <ul
        className="grid"
        style={{ gridTemplateColumns: `repeat(${items.length}, minmax(0, 1fr))` }}
      >
        {items.map(({ to, label, icon: Icon, end }) => (
          <li key={to}>
            <NavLink
              to={to}
              end={end}
              className={({ isActive }) =>
                cn(
                  // 56px tall: comfortably past the 44px minimum touch target,
                  // and enough for an icon with a readable label under it.
                  'flex h-14 flex-col items-center justify-center gap-1 text-[10px] font-medium transition-colors',
                  isActive ? 'text-primary' : 'text-muted-foreground',
                )
              }
            >
              {({ isActive }) => (
                <>
                  <Icon className={cn('size-5', isActive ? 'opacity-100' : 'opacity-70')} />
                  <span className="max-w-full truncate px-1">{label}</span>
                </>
              )}
            </NavLink>
          </li>
        ))}
      </ul>
    </nav>
  );
}
