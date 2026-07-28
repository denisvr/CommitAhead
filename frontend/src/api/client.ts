import createClient from 'openapi-fetch'
import type { paths } from './generated/schema'

// The API is always same-origin (Kestrel serves the production build; Vite's dev proxy forwards
// /api and /auth locally) — window.location.origin resolves correctly in every environment,
// including jsdom in tests, where a bare relative baseUrl fails to construct a valid Request.
export const apiClient = createClient<paths>({ baseUrl: window.location.origin, credentials: 'include' })

const RetryHeader = 'X-CommitAhead-Retry'

let refreshInFlight: Promise<boolean> | null = null

/**
 * Single-flight refresh: concurrent callers share one /auth/refresh call instead of racing each
 * other. Exported so the logout flow can also call it directly (attempt a fresh access token
 * before revoking, since Supabase revocation needs one) without going through a 401 first.
 */
export function ensureFreshSession(): Promise<boolean> {
  refreshInFlight ??= (async () => {
    const { data: csrf } = await apiClient.GET('/auth/csrf')
    if (!csrf) {
      return false
    }

    const { response } = await apiClient.POST('/auth/refresh', {
      headers: { 'X-CSRF-TOKEN': csrf.token },
    })

    return response.status === 204
  })().finally(() => {
    refreshInFlight = null
  })

  return refreshInFlight
}

const pendingRequestClones = new Map<string, Request>()

apiClient.use({
  onRequest({ request, id }) {
    // Cloned before the request is sent — a Request's body stream can only be read once, so
    // cloning after the fact (in onResponse, once fetch has already consumed it) would throw for
    // any future request with a body. GET requests like /api/me have no body, but this keeps the
    // retry path correct for state-changing endpoints too.
    pendingRequestClones.set(id, request.clone())
  },
  async onResponse({ request, response, id }) {
    const clone = pendingRequestClones.get(id)
    pendingRequestClones.delete(id)

    // /auth/* endpoints (including /auth/refresh itself) manage their own flow — retrying them
    // here would recurse into ensureFreshSession while it's still awaiting its own /auth/refresh
    // call, deadlocking on the in-flight promise.
    if (response.status !== 401 || request.headers.has(RetryHeader) || !clone || new URL(request.url).pathname.startsWith('/auth/')) {
      return response
    }

    const refreshed = await ensureFreshSession()
    if (!refreshed) {
      return response
    }

    // Calling the global fetch directly (not apiClient) retries exactly once and never re-enters
    // this middleware, so a request that fails again after a successful refresh just surfaces
    // its second 401 — no retry loop.
    const retryRequest = new Request(clone)
    retryRequest.headers.set(RetryHeader, '1')
    return fetch(retryRequest)
  },
})
