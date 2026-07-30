import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import App from './App'

const { getMock, postMock, ensureFreshSessionMock } = vi.hoisted(() => ({
  getMock: vi.fn(),
  postMock: vi.fn(),
  ensureFreshSessionMock: vi.fn(),
}))

vi.mock('./api/client', () => ({
  apiClient: { GET: getMock, POST: postMock },
  ensureFreshSession: ensureFreshSessionMock,
}))

function mockAuthenticated() {
  getMock.mockImplementation((path: string) => {
    if (path === '/api/me') {
      return Promise.resolve({ data: { email: 'owner@example.com' }, response: new Response(null, { status: 200 }) })
    }
    if (path === '/auth/csrf') {
      return Promise.resolve({ data: { token: 'csrf-token' }, response: new Response(null, { status: 200 }) })
    }
    if (path === '/api/study-queue') {
      return Promise.resolve({ data: [], response: new Response(null, { status: 200 }) })
    }
    return Promise.resolve({ data: undefined, response: new Response(null, { status: 404 }) })
  })
}

describe('App', () => {
  beforeEach(() => {
    getMock.mockReset()
    postMock.mockReset()
    ensureFreshSessionMock.mockReset()
  })

  it('renders the CommitAhead heading', () => {
    getMock.mockResolvedValue({ data: undefined, response: new Response(null, { status: 401 }) })

    render(<App />)

    expect(screen.getByRole('heading', { name: 'CommitAhead' })).toBeInTheDocument()
  })

  it('shows the login form once the anonymous /api/me check resolves', async () => {
    getMock.mockResolvedValue({ data: undefined, response: new Response(null, { status: 401 }) })

    render(<App />)

    expect(await screen.findByLabelText('Email')).toBeInTheDocument()
  })

  it('shows the study queue and the signed-in email once authenticated', async () => {
    mockAuthenticated()

    render(<App />)

    expect(await screen.findByRole('heading', { name: 'Study queue' })).toBeInTheDocument()
    expect(screen.getByText('owner@example.com')).toBeInTheDocument()
  })

  it('logging out attempts a refresh first, then calls csrf and logout, then shows the login form', async () => {
    mockAuthenticated()
    postMock.mockResolvedValue({ data: undefined, response: new Response(null, { status: 204 }) })
    ensureFreshSessionMock.mockResolvedValue(true)

    render(<App />)
    expect(await screen.findByText('owner@example.com')).toBeInTheDocument()

    await userEvent.click(screen.getByRole('button', { name: 'Log out' }))

    expect(await screen.findByLabelText('Email')).toBeInTheDocument()
    expect(postMock).toHaveBeenCalledWith('/auth/logout', { headers: { 'X-CSRF-TOKEN': 'csrf-token' } })
    // ensureFreshSession must run before the logout POST, not after — refresh gives /auth/logout
    // a valid access token to revoke.
    expect(ensureFreshSessionMock.mock.invocationCallOrder[0]).toBeLessThan(postMock.mock.invocationCallOrder[0])
  })

  it('does not switch to anonymous when the CSRF fetch fails — shows a retryable error instead', async () => {
    getMock.mockImplementation((path: string) => {
      if (path === '/api/me') {
        return Promise.resolve({ data: { email: 'owner@example.com' }, response: new Response(null, { status: 200 }) })
      }
      if (path === '/auth/csrf') {
        return Promise.resolve({ data: undefined, response: new Response(null, { status: 500 }) })
      }
      if (path === '/api/study-queue') {
        return Promise.resolve({ data: [], response: new Response(null, { status: 200 }) })
      }
      return Promise.resolve({ data: undefined, response: new Response(null, { status: 404 }) })
    })
    ensureFreshSessionMock.mockResolvedValue(false)

    render(<App />)
    expect(await screen.findByText('owner@example.com')).toBeInTheDocument()

    await userEvent.click(screen.getByRole('button', { name: 'Log out' }))

    expect(await screen.findByRole('alert')).toHaveTextContent(/try again/i)
    expect(screen.queryByLabelText('Email')).not.toBeInTheDocument()
    expect(screen.getByText('owner@example.com')).toBeInTheDocument()
    expect(postMock).not.toHaveBeenCalledWith('/auth/logout', expect.anything())
  })

  it('does not switch to anonymous when /auth/logout itself returns a non-2xx status', async () => {
    mockAuthenticated()
    postMock.mockResolvedValue({ data: undefined, response: new Response(null, { status: 400 }) })
    ensureFreshSessionMock.mockResolvedValue(true)

    render(<App />)
    expect(await screen.findByText('owner@example.com')).toBeInTheDocument()

    await userEvent.click(screen.getByRole('button', { name: 'Log out' }))

    expect(await screen.findByRole('alert')).toHaveTextContent(/try again/i)
    expect(screen.getByText('owner@example.com')).toBeInTheDocument()
  })

  it('does not switch to anonymous when the /auth/logout call throws (network failure)', async () => {
    mockAuthenticated()
    postMock.mockRejectedValue(new TypeError('Failed to fetch'))
    ensureFreshSessionMock.mockResolvedValue(true)

    render(<App />)
    expect(await screen.findByText('owner@example.com')).toBeInTheDocument()

    await userEvent.click(screen.getByRole('button', { name: 'Log out' }))

    expect(await screen.findByRole('alert')).toHaveTextContent(/try again/i)
    expect(screen.getByText('owner@example.com')).toBeInTheDocument()
  })

  it('allows retrying logout after a failure, and succeeds the second time', async () => {
    mockAuthenticated()
    ensureFreshSessionMock.mockResolvedValue(true)
    postMock
      .mockResolvedValueOnce({ data: undefined, response: new Response(null, { status: 500 }) })
      .mockResolvedValueOnce({ data: undefined, response: new Response(null, { status: 204 }) })

    render(<App />)
    expect(await screen.findByText('owner@example.com')).toBeInTheDocument()

    await userEvent.click(screen.getByRole('button', { name: 'Log out' }))
    expect(await screen.findByRole('alert')).toBeInTheDocument()

    await userEvent.click(screen.getByRole('button', { name: 'Log out' }))

    expect(await screen.findByLabelText('Email')).toBeInTheDocument()
  })
})
