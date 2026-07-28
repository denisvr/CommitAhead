import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

function jsonResponse(body: unknown, status: number) {
  return new Response(JSON.stringify(body), { status, headers: { 'content-type': 'application/json' } })
}

describe('apiClient single-flight refresh-and-retry on 401', () => {
  let fetchMock: ReturnType<typeof vi.fn>

  beforeEach(() => {
    vi.resetModules()
    fetchMock = vi.fn()
    vi.stubGlobal('fetch', fetchMock)
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('refreshes once and retries the original request after a 401', async () => {
    fetchMock
      .mockResolvedValueOnce(new Response(null, { status: 401 })) // GET /api/me
      .mockResolvedValueOnce(jsonResponse({ token: 'csrf-token' }, 200)) // GET /auth/csrf
      .mockResolvedValueOnce(new Response(null, { status: 204 })) // POST /auth/refresh
      .mockResolvedValueOnce(jsonResponse({ email: 'owner@example.com' }, 200)) // retried GET /api/me

    const { apiClient } = await import('./client')
    const result = await apiClient.GET('/api/me')

    expect(result.response.status).toBe(200)
    expect(result.data).toEqual({ email: 'owner@example.com' })
    expect(fetchMock).toHaveBeenCalledTimes(4)
  })

  it('returns the original 401 without retrying when refresh fails', async () => {
    fetchMock
      .mockResolvedValueOnce(new Response(null, { status: 401 })) // GET /api/me
      .mockResolvedValueOnce(jsonResponse({ token: 'csrf-token' }, 200)) // GET /auth/csrf
      .mockResolvedValueOnce(new Response(null, { status: 401 })) // POST /auth/refresh fails

    const { apiClient } = await import('./client')
    const result = await apiClient.GET('/api/me')

    expect(result.response.status).toBe(401)
    // Exactly the 3 calls above — no infinite loop, no second retry attempt.
    expect(fetchMock).toHaveBeenCalledTimes(3)
  })

  it('shares one in-flight refresh across concurrent 401s', async () => {
    // openapi-fetch always invokes its fetch as fetch(request, ...) with a real Request object
    // (see its source), and so does this client's own retry — branch on the Request directly.
    fetchMock.mockImplementation((request: Request) => {
      if (request.url.includes('/auth/csrf')) {
        return Promise.resolve(jsonResponse({ token: 'csrf-token' }, 200))
      }
      if (request.url.includes('/auth/refresh')) {
        return Promise.resolve(new Response(null, { status: 204 }))
      }
      if (request.url.includes('/api/me')) {
        const isRetry = request.headers.has('X-CommitAhead-Retry')
        return Promise.resolve(isRetry ? jsonResponse({ email: 'owner@example.com' }, 200) : new Response(null, { status: 401 }))
      }
      return Promise.resolve(new Response(null, { status: 401 }))
    })

    const { apiClient } = await import('./client')
    const [first, second] = await Promise.all([apiClient.GET('/api/me'), apiClient.GET('/api/me')])

    expect(first.response.status).toBe(200)
    expect(second.response.status).toBe(200)
    const refreshCalls = fetchMock.mock.calls.filter(([request]) => (request as Request).url.includes('/auth/refresh'))
    expect(refreshCalls).toHaveLength(1)
  })

  it('ensureFreshSession resolves to false instead of rejecting when the network call throws', async () => {
    fetchMock.mockRejectedValue(new TypeError('Failed to fetch'))

    const { ensureFreshSession } = await import('./client')

    await expect(ensureFreshSession()).resolves.toBe(false)
  })

  it('falls back to the original 401 response when the retry attempt itself throws', async () => {
    fetchMock
      .mockResolvedValueOnce(new Response(null, { status: 401 })) // GET /api/me
      .mockResolvedValueOnce(jsonResponse({ token: 'csrf-token' }, 200)) // GET /auth/csrf
      .mockResolvedValueOnce(new Response(null, { status: 204 })) // POST /auth/refresh
      .mockRejectedValueOnce(new TypeError('Failed to fetch')) // retried GET /api/me throws

    const { apiClient } = await import('./client')
    const result = await apiClient.GET('/api/me')

    expect(result.response.status).toBe(401)
  })

  it('does not leave the request-clone map inconsistent when a call throws — a later call still succeeds', async () => {
    fetchMock
      .mockRejectedValueOnce(new TypeError('Failed to fetch')) // first GET /api/me throws
      .mockResolvedValueOnce(jsonResponse({ email: 'owner@example.com' }, 200)) // second GET /api/me succeeds

    const { apiClient } = await import('./client')

    await expect(apiClient.GET('/api/me')).rejects.toThrow('Failed to fetch')

    const result = await apiClient.GET('/api/me')
    expect(result.response.status).toBe(200)
  })
})
