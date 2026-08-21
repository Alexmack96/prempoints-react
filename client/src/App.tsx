import { Outlet } from 'react-router-dom';
import NavBar from './features/shared/NavBar';

function App() {
  return (
    <div className="flex min-h-dvh flex-col">
      <NavBar />
      <main className="mx-auto w-full max-w-7xl flex-1 px-4 py-8 sm:px-6 lg:px-8">
        <Outlet />
      </main>
      <footer className="mx-auto w-full max-w-7xl px-4 pb-8 sm:px-6 lg:px-8">
        <p className="text-muted-foreground text-xs">
          PremPoints 2025/26 &middot; prices settle on the mid &middot; stakes total 40
        </p>
      </footer>
    </div>
  );
}

export default App;
