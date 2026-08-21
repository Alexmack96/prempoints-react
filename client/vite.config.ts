import { fileURLToPath, URL } from 'node:url'
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
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
    },
  },
  server: {
    // The AppHost pins this and passes it as PORT. The fallback matches it so
    // `bun run dev` on its own lands on the same origin — WorkOS only redirects
    // back to a registered URI, and one registration should cover both ways of
    // starting the client.
    port: Number(process.env.PORT) || 57966,
    // The client calls /api/... same-origin and this forwards it to the .NET
    // API, so CORS never comes into it. API_URL is injected by the AppHost.
    proxy: {
      '/api': process.env.API_URL ?? 'http://localhost:5062',
    },
  },
})
