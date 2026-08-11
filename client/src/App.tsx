// src/App.tsx
import { Outlet } from 'react-router-dom';
import NavBar from './features/shared/NavBar';
import { ReactQueryDevtools } from '@tanstack/react-query-devtools';

function App() {
  return (
    <>
      <NavBar />
      <main className="container mx-auto p-4">
        {/* This is your @RenderBody() */}
        <Outlet />
      </main>
    </>
  );
}
export default App;
