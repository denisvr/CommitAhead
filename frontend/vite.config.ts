import { defineConfig } from 'vitest/config'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  build: {
    outDir: 'dist',
  },
  server: {
    proxy: {
      '/api': {
        target: 'http://localhost:5120',
        changeOrigin: true,
      },
      '/auth': {
        target: 'http://localhost:5120',
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
