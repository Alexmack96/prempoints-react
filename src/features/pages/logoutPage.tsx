import { useEffect } from 'react';
import { Link } from 'react-router-dom';

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
    <div className="flex min-h-[80vh] flex-col items-center justify-center px-4 text-center">
      {/* Icon Wrapper */}
      <div className="mb-6 rounded-full bg-purple-100 p-6">
        {/* SVG Logout Icon */}
        <svg
          xmlns="http://www.w3.org/2000/svg"
          width="48"
          height="48"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          strokeWidth="2"
          strokeLinecap="round"
          strokeLinejoin="round"
          className="text-purple-800"
        >
          <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4" />
          <polyline points="16 17 21 12 16 7" />
          <line x1="21" y1="12" x2="9" y2="12" />
        </svg>
      </div>

      {/* Main Text */}
      <h1 className="mb-2 text-3xl font-bold text-gray-800">You have been logged out</h1>
      <p className="mb-8 text-gray-600">
        Thank you for visiting PremPoints 2025/26. We hope to see you again soon!
      </p>

      {/* Action Buttons */}
      <div className="flex flex-col gap-4 sm:flex-row">
        <Link
          to="/login"
          className="rounded-lg bg-fuchsia-800 px-6 py-3 font-semibold text-white transition hover:bg-fuchsia-900 focus:outline-hidden focus:ring-2 focus:ring-fuchsia-500 focus:ring-offset-2"
        >
          Sign In Again
        </Link>

        <Link
          to="/"
          className="rounded-lg border border-gray-300 bg-white px-6 py-3 font-semibold text-gray-700 transition hover:bg-gray-50 focus:outline-hidden focus:ring-2 focus:ring-gray-200 focus:ring-offset-2"
        >
          Return Home
        </Link>
      </div>
    </div>
  );
}
