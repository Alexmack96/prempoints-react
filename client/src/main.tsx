import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { createBrowserRouter, RouterProvider } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'; // 1. Import
import { ReactQueryDevtools } from '@tanstack/react-query-devtools'; // 2. Optional: DevTools
import App from './App.tsx';
import './index.css';
import TeamsPage from './features/pages/teamsPage.tsx';
import PricesPage from './features/pages/pricesPage.tsx';

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
      { index: true, element: <div className="p-3">Home</div> },
      { path: 'leaderboard', element: <div className="p-3">Leaderboard</div> },
      { path: 'teams', element: <TeamsPage /> },
      { path: 'prices', element: <PricesPage /> },
      { path: 'results', element: <div className="p-3">Results</div> },
    ],
  },
]);

//app.run() equivalent
createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <QueryClientProvider client={queryClient}>
      <RouterProvider router={router} />
      <ReactQueryDevtools initialIsOpen={false} />
    </QueryClientProvider>
  </StrictMode>,
);
