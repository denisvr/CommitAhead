import { http, HttpResponse } from 'msw'

// The happy-path default for every endpoint an API-bound component test exercises
// (docs/testing/strategy.md "MSW provides representative success, loading, validation,
// unauthorised, and server-error variants per flow"). Individual tests override one handler at a
// time with `server.use(...)` for the variant they're asserting on.
export const DEFAULT_EMAIL = 'owner@example.com'

export const handlers = [
  http.get('/api/me', () => HttpResponse.json({ email: DEFAULT_EMAIL })),
  http.post('/auth/login', () => HttpResponse.json({ message: 'If that email is registered, a sign-in link has been sent.' })),
  http.get('/auth/csrf', () => HttpResponse.json({ token: 'csrf-token' })),
  http.post('/auth/refresh', () => new HttpResponse(null, { status: 204 })),
  http.post('/auth/logout', () => new HttpResponse(null, { status: 204 })),
  http.get('/api/professional-profile', () => new HttpResponse(null, { status: 404 })),
  http.get('/api/cv-presentations', () => HttpResponse.json([])),
]
