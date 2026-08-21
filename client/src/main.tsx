import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { createBrowserRouter, RouterProvider } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'; // 1. Import
import { ReactQueryDevtools } from '@tanstack/react-query-devtools'; // 2. Optional: DevTools
import { AuthKitProvider } from '@workos-inc/authkit-react';
import { Shield } from 'lucide-react';
import App from './App.tsx';
import { AuthBridge } from './features/auth/AuthBridge.tsx';
import { ThemeProvider } from './components/theme/ThemeProvider.tsx';
import { workOsClientId } from './lib/authConfig.ts';
import './index.css';
import AdminPage from './features/pages/adminPage.tsx';
import LeaderboardPage from './features/pages/leaderboardPage.tsx';
import PricesPage from './features/pages/pricesPage.tsx';
import Logout from './features/pages/logoutPage.tsx';
import TradeBoard from './features/trades/TradeBoard.tsx';
import { ComingSoon } from './features/shared/ComingSoon.tsx';
import { RequireAdmin } from './features/auth/RequireAdmin.tsx';

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 1000 * 60 * 5, // Optional: 5 minutes stale time
    },
  },
});

//app.mapcontrollers() equivalent
const router = createBrowserRouter([
  {
    path: '/',
    element: <App />,
    children: [
      { index: true, element: <TradeBoard /> },
      { path: 'leaderboard', element: <LeaderboardPage /> },
      // Guarded here as well as hidden from the nav: a link nobody can see is
      // still a URL anybody can type.
      {
        path: 'admin',
        element: (
          <RequireAdmin>
            <AdminPage />
          </RequireAdmin>
        ),
      },
      { path: 'prices', element: <PricesPage /> },
      {
        path: 'results',
        element: (
          <ComingSoon
            title="Results"
            description="Matchweek results and what each one did to the prices."
            icon={Shield}
          />
        ),
      },
      // The nav and Ctrl+Alt+0 both point here, so it needs a route of its own.
      { path: 'logout', element: <Logout /> },
    ],
  },
]);

//app.run() equivalent
createRoot(document.getElementById('root')!).render(
  <StrictMode>
    {/* AuthKit outermost: the token getter it publishes has to be in place
        before any query fires, or the first request goes out unauthenticated. */}
    <AuthKitProvider clientId={workOsClientId}>
      <AuthBridge>
        <QueryClientProvider client={queryClient}>
          <ThemeProvider>
            <RouterProvider router={router} />
          </ThemeProvider>
          <ReactQueryDevtools initialIsOpen={false} />
        </QueryClientProvider>
      </AuthBridge>
    </AuthKitProvider>
  </StrictMode>,
);
