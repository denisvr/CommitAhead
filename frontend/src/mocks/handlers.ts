import { http, HttpResponse } from 'msw'

// The happy-path default for every endpoint an API-bound component test exercises
// (docs/testing/strategy.md "MSW provides representative success, loading, validation,
// unauthorised, and server-error variants per flow"). Individual tests override one handler at a
// time with `server.use(...)` for the variant they're asserting on.
export const DEFAULT_EMAIL = 'owner@example.com'

export const DEFAULT_SCORING_CONFIG = { importanceWeight: 40, demandWeight: 35, masteryGapWeight: 25, isOverridden: false }

export const handlers = [
  http.get('/api/me', () => HttpResponse.json({ email: DEFAULT_EMAIL })),
  http.post('/auth/login', () => HttpResponse.json({ message: 'If that email is registered, a sign-in link has been sent.' })),
  http.get('/auth/csrf', () => HttpResponse.json({ token: 'csrf-token' })),
  http.post('/auth/refresh', () => new HttpResponse(null, { status: 204 })),
  http.post('/auth/logout', () => new HttpResponse(null, { status: 204 })),
  http.get('/api/study-queue', () => HttpResponse.json([])),
  http.get('/api/study-items', () => HttpResponse.json([])),
  http.get('/api/scoring-config', () => HttpResponse.json(DEFAULT_SCORING_CONFIG)),
  http.put('/api/scoring-config', () => new HttpResponse(null, { status: 204 })),
  http.delete('/api/scoring-config', () => new HttpResponse(null, { status: 204 })),
]
