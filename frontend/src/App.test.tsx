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

  it('logging out attempts a refresh first, then calls csrf and logout, then shows the login form', async () => {
    getMock.mockImplementation((path: string) => {
      if (path === '/api/me') {
        return Promise.resolve({ data: { email: 'owner@example.com' }, response: new Response(null, { status: 200 }) })
      }
      if (path === '/auth/csrf') {
        return Promise.resolve({ data: { token: 'csrf-token' }, response: new Response(null, { status: 200 }) })
      }
      return Promise.resolve({ data: undefined, response: new Response(null, { status: 404 }) })
    })
    postMock.mockResolvedValue({ data: undefined, response: new Response(null, { status: 204 }) })
    ensureFreshSessionMock.mockResolvedValue(true)

    render(<App />)
    expect(await screen.findByText('Signed in as owner@example.com')).toBeInTheDocument()

    await userEvent.click(screen.getByRole('button', { name: 'Log out' }))

    expect(await screen.findByLabelText('Email')).toBeInTheDocument()
    expect(postMock).toHaveBeenCalledWith('/auth/logout', { headers: { 'X-CSRF-TOKEN': 'csrf-token' } })
    // ensureFreshSession must run before the logout POST, not after — refresh gives /auth/logout
    // a valid access token to revoke.
    expect(ensureFreshSessionMock.mock.invocationCallOrder[0]).toBeLessThan(postMock.mock.invocationCallOrder[0])
  })

  it('logging out still clears local state even when the CSRF fetch fails', async () => {
    getMock.mockImplementation((path: string) => {
      if (path === '/api/me') {
        return Promise.resolve({ data: { email: 'owner@example.com' }, response: new Response(null, { status: 200 }) })
      }
      if (path === '/auth/csrf') {
        return Promise.resolve({ data: undefined, response: new Response(null, { status: 500 }) })
      }
      return Promise.resolve({ data: undefined, response: new Response(null, { status: 404 }) })
    })
    ensureFreshSessionMock.mockResolvedValue(false)

    render(<App />)
    expect(await screen.findByText('Signed in as owner@example.com')).toBeInTheDocument()

    await userEvent.click(screen.getByRole('button', { name: 'Log out' }))

    expect(await screen.findByLabelText('Email')).toBeInTheDocument()
    expect(postMock).not.toHaveBeenCalledWith('/auth/logout', expect.anything())
  })
})
