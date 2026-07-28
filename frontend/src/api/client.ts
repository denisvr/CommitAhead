import createClient from 'openapi-fetch'
import type { paths } from './generated/schema'

// The API is always same-origin (Kestrel serves the production build; Vite's dev proxy forwards
// /api and /auth locally) — window.location.origin resolves correctly in every environment,
// including jsdom in tests, where a bare relative baseUrl fails to construct a valid Request.
export const apiClient = createClient<paths>({ baseUrl: window.location.origin, credentials: 'include' })
