import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { createBrowserRouter, RouterProvider } from "react-router-dom";
import App from "./App.tsx";
import "./index.css";

const router = createBrowserRouter([
  {
    path: "/",
    element: <App />,
    children: [
      { index: true, element: <div className="p-6">Home</div> },
      { path: "leaderboard", element: <div className="p-6">Leaderboard</div> },
      { path: "prices", element: <div className="p-6">Prices</div> },
      { path: "results", element: <div className="p-6">Results</div> },
    ],
  },
]);

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <RouterProvider router={router} />
  </StrictMode>,
);
