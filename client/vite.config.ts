import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [
    react({
      babel: {
        plugins: [['babel-plugin-react-compiler']],
      },
    }),
    tailwindcss(),
  ],
  server: {
    // Aspire's AddViteApp assigns the port and passes it as PORT. The fallback
    // is for running `bun run dev` on its own.
    port: Number(process.env.PORT) || 5173,
    // The client calls /api/... same-origin and this forwards it to the .NET
    // API, so CORS never comes into it. API_URL is injected by the AppHost.
    proxy: {
      '/api': process.env.API_URL ?? 'http://localhost:5062',
    },
  },
})
