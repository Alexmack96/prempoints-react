import { useEffect } from 'react';
import { NavLink, useNavigate } from 'react-router-dom';
// Ideally, swap this absolute path for a relative one (e.g., '../assets/prem_lion.jpg') for better portability
import image_prem_lion from '../../assets/prem_lion.jpg';

export default function NavBar() {
  const navigate = useNavigate();

  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.ctrlKey && e.altKey) {
        switch (e.key) {
          case '1':
            e.preventDefault(); // Good practice to prevent default browser behavior for shortcuts
            navigate('/');
            break;
          case '2':
            e.preventDefault();
            navigate('/leaderboard');
            break;
          case '3':
            e.preventDefault();
            navigate('/prices');
            break;
          case '4':
            e.preventDefault();
            navigate('/results');
            break;
          case '5':
            e.preventDefault();
            navigate('/teams');
            break;
          case '0':
            e.preventDefault();
            navigate('/logout');
            break;
          default:
            break;
        }
      }
    };

    // Add event listener
    window.addEventListener('keydown', handleKeyDown);

    // Clean up
    return () => {
      window.removeEventListener('keydown', handleKeyDown);
    };
  }, [navigate]);

  return (
    <header>
      <nav className="w-full border-b bg-linear-to-r from-fuchsia-800 to-purple-950 text-white font-bold">
        <div className="mx-auto flex h-16 items-center justify-between px-4">
          {/* Left: Logo */}
          <div className="flex items-center gap-2">
            <img src={image_prem_lion} alt="Prem Lion" className="h-10 w-10 rounded-full" />
            <span className="text-lg">PremPoints 2025/26</span>
          </div>

          {/* Center: Links */}
          <ul className="flex items-center gap-6">
            <li>
              <NavLink to="/" className="hover:text-purple-200" end title="Ctrl+Alt+1">
                Home
              </NavLink>
            </li>
            <li>
              <NavLink to="/leaderboard" className="hover:text-purple-200" title="Ctrl+Alt+2">
                Leaderboard
              </NavLink>
            </li>
            <li>
              <NavLink to="/prices" className="hover:text-purple-200" title="Ctrl+Alt+3">
                Prices
              </NavLink>
            </li>
            <li>
              <NavLink to="/results" className="hover:text-purple-200" title="Ctrl+Alt+4">
                Results
              </NavLink>
            </li>
            <li>
              <NavLink to="/teams" className="hover:text-purple-200" title="Ctrl+Alt+5">
                Teams
              </NavLink>
            </li>
          </ul>

          {/* Right: Logout */}
          <div className="flex items-center">
            <NavLink to="/logout" className="hover:text-purple-200">
              Log Out
            </NavLink>
          </div>
        </div>
      </nav>
    </header>
  );
}
