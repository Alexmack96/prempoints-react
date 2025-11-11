import { NavLink } from 'react-router-dom';

export default function NavBar() {
  return (
    <header
      style={{
        position: 'sticky',
        top: 0,
        background: '#fff',
        borderBottom: '1px solid #eee',
        zIndex: 50,
      }}
    >
      <nav style={{ maxWidth: 960, margin: '0 auto', padding: '0 16px' }} aria-label="Primary">
        <ul
          style={{
            display: 'flex',
            gap: 16,
            height: 56,
            alignItems: 'center',
            margin: 0,
            padding: 0,
            listStyle: 'none',
          }}
        >
          <li>
            <NavLink
              to="/"
              end
              className={({ isActive }) => (isActive ? 'active-link' : undefined)}
            >
              Home
            </NavLink>
          </li>
          <li>
            <NavLink
              to="/leaderboard"
              className={({ isActive }) => (isActive ? 'active-link' : undefined)}
            >
              Leaderboard
            </NavLink>
          </li>
          <li>
            <NavLink
              to="/prices"
              className={({ isActive }) => (isActive ? 'active-link' : undefined)}
            >
              Prices
            </NavLink>
          </li>
          <li>
            <NavLink
              to="/results"
              className={({ isActive }) => (isActive ? 'active-link' : undefined)}
            >
              Results
            </NavLink>
          </li>
        </ul>
      </nav>
    </header>
  );
}
