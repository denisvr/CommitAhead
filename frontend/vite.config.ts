import { defineConfig } from 'vitest/config'
import react from '@vitejs/plugin-react'

// Host `dotnet run` (launchSettings.json) listens on localhost:5120 — the default. Running inside
// docker-compose.dev.yml's `frontend` container, "localhost" would mean this very container, not
// the `api` one, so that stack sets VITE_DEV_API_PROXY_TARGET to the Compose service name instead
// (see that file's own comment for why). Only affects `vite dev`; the production build proxies
// nothing — Kestrel serves the SPA and the API from the same origin.
const devApiProxyTarget = process.env.VITE_DEV_API_PROXY_TARGET ?? 'http://localhost:5120'

export default defineConfig({
  plugins: [react()],
  build: {
    outDir: 'dist',
  },
  server: {
    proxy: {
      '/api': {
        target: devApiProxyTarget,
        changeOrigin: true,
      },
      '/auth': {
        target: devApiProxyTarget,
        changeOrigin: true,
      },
    },
  },
  test: {
    environment: 'jsdom',
    setupFiles: ['./src/test-setup.ts'],
    // Node's fetch/Request (used under jsdom too — jsdom itself has no Fetch implementation)
    // can't resolve a relative URL the way a real browser resolves one against document.baseURI.
    // This gives the test run an absolute base to read via import.meta.env; production/dev builds
    // never set it, so the real app stays on the relative, same-origin baseUrl (see api/client.ts).
    env: {
      VITE_API_BASE_URL: 'http://localhost:3000',
    },
  },
})
