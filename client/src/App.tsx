import { Outlet } from 'react-router-dom';
import NavBar from './features/shared/NavBar';
import { useSeasonLabel } from './features/seasons/useCurrentSeason';
import { OnboardingGate } from './features/onboarding/OnboardingGate';

function App() {
  const season = useSeasonLabel();

  return (
    // pb-14 on mobile clears the fixed bottom tab bar, which would otherwise
    // sit over the end of every page. Dropped from md up, where the bar is
    // hidden and the space would just be a gap.
    <div className="flex min-h-dvh flex-col pb-14 md:pb-0">
      <NavBar />
      <main className="mx-auto w-full max-w-7xl flex-1 px-4 py-8 sm:px-6 lg:px-8">
        <OnboardingGate>
          <Outlet />
        </OnboardingGate>
      </main>
      <footer className="mx-auto w-full max-w-7xl px-4 pb-8 sm:px-6 lg:px-8">
        <p className="text-muted-foreground text-xs">
          PremPoints{season ? ` ${season}` : ''} &middot; prices settle on the mid &middot; stakes total 40
        </p>
      </footer>
    </div>
  );
}

export default App;
