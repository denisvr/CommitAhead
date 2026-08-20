import { describe, it, expect } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { server } from './mocks/server'
import App from './App'

// The Studio shell renders each control exactly once. The Reading Room shell duplicated its nav
// and logout into a desktop sidebar and a mobile bottom bar, which forced getAllByRole(...)[0]
// here because jsdom has no layout engine to hide either copy; that is no longer the case.
//
// Email and Log out only render once the account avatar is opened (AccountMenu) — this helper
// opens it first, tolerating it already being open (the retry-logout test calls this twice).
async function openAccountMenu() {
  const trigger = await screen.findByRole('button', { name: 'Account menu' })
  if (trigger.getAttribute('aria-expanded') !== 'true') await userEvent.click(trigger)
}

async function clickLogout() {
  await openAccountMenu()
  await userEvent.click(await screen.findByRole('button', { name: /log out/i }))
}

describe('App', () => {
  it('renders the CommitAhead heading', () => {
    server.use(http.get('/api/me', () => new HttpResponse(null, { status: 401 })))

    render(<App />)

    expect(screen.getByRole('heading', { name: 'CommitAhead' })).toBeInTheDocument()
  })

  it('shows the login form once the anonymous /api/me check resolves', async () => {
    server.use(http.get('/api/me', () => new HttpResponse(null, { status: 401 })))

    render(<App />)

    expect(await screen.findByLabelText('Email')).toBeInTheDocument()
  })

  it('shows a retryable connection error when the initial /api/me check throws, never the login form', async () => {
    server.use(http.get('/api/me', () => HttpResponse.error()))

    render(<App />)

    expect(await screen.findByRole('alert')).toHaveTextContent(/could not reach commitahead/i)
    expect(screen.queryByLabelText('Email')).not.toBeInTheDocument()
  })

  it('retrying after a connection error re-checks /api/me and succeeds', async () => {
    let callCount = 0
    server.use(
      http.get('/api/me', () => {
        callCount += 1
        return callCount === 1 ? HttpResponse.error() : new HttpResponse(null, { status: 401 })
      }),
    )

    render(<App />)
    expect(await screen.findByRole('alert')).toBeInTheDocument()

    await userEvent.click(screen.getByRole('button', { name: 'Try again' }))

    expect(await screen.findByLabelText('Email')).toBeInTheDocument()
  })

  it('shows the professional profile and the signed-in email once authenticated', async () => {
    render(<App />)

    // Professional profile & CVs is the only feature in the app (see App.tsx's SIDEBAR_ITEMS).
    expect(await screen.findByRole('heading', { name: /professional profile/i })).toBeInTheDocument()
    await openAccountMenu()
    expect(screen.getByText('owner@example.com')).toBeInTheDocument()
  })

  it('logging out attempts a refresh first, then calls csrf and logout, then shows the login form', async () => {
    const calledPaths: string[] = []
    server.use(
      http.post('/auth/refresh', () => {
        calledPaths.push('/auth/refresh')
        return new HttpResponse(null, { status: 204 })
      }),
      http.post('/auth/logout', () => {
        calledPaths.push('/auth/logout')
        return new HttpResponse(null, { status: 204 })
      }),
    )

    render(<App />)
    await openAccountMenu()
    expect(screen.getByText('owner@example.com')).toBeInTheDocument()

    await clickLogout()

    expect(await screen.findByLabelText('Email')).toBeInTheDocument()
    // ensureFreshSession's refresh must run before the logout POST, not after — refresh gives
    // /auth/logout a valid access token to revoke.
    expect(calledPaths).toEqual(['/auth/refresh', '/auth/logout'])
  })

  it('does not switch to anonymous when the CSRF fetch fails — shows a retryable error instead', async () => {
    server.use(http.get('/auth/csrf', () => new HttpResponse(null, { status: 500 })))

    render(<App />)
    await openAccountMenu()
    expect(screen.getByText('owner@example.com')).toBeInTheDocument()

    await clickLogout()

    expect(await screen.findByRole('alert')).toHaveTextContent(/try again/i)
    // Not `queryByLabelText('Email')` — the default landing view (Professional profile) has its
    // own contact-info field with that same accessible label, so it's not a reliable signal that
    // the *login* form specifically is absent. The "Send sign-in link" submit button is unique to it.
    expect(screen.queryByRole('button', { name: 'Send sign-in link' })).not.toBeInTheDocument()
    expect(screen.getByText('owner@example.com')).toBeInTheDocument()
  })

  it('does not switch to anonymous when /auth/logout itself returns a non-2xx status', async () => {
    server.use(http.post('/auth/logout', () => new HttpResponse(null, { status: 400 })))

    render(<App />)
    await openAccountMenu()
    expect(screen.getByText('owner@example.com')).toBeInTheDocument()

    await clickLogout()

    expect(await screen.findByRole('alert')).toHaveTextContent(/try again/i)
    expect(screen.getByText('owner@example.com')).toBeInTheDocument()
  })

  it('does not switch to anonymous when the /auth/logout call throws (network failure)', async () => {
    server.use(http.post('/auth/logout', () => HttpResponse.error()))

    render(<App />)
    await openAccountMenu()
    expect(screen.getByText('owner@example.com')).toBeInTheDocument()

    await clickLogout()

    expect(await screen.findByRole('alert')).toHaveTextContent(/try again/i)
    expect(screen.getByText('owner@example.com')).toBeInTheDocument()
  })

  it('allows retrying logout after a failure, and succeeds the second time', async () => {
    let callCount = 0
    server.use(
      http.post('/auth/logout', () => {
        callCount += 1
        return new HttpResponse(null, { status: callCount === 1 ? 500 : 204 })
      }),
    )

    render(<App />)
    await openAccountMenu()
    expect(screen.getByText('owner@example.com')).toBeInTheDocument()

    await clickLogout()
    expect(await screen.findByRole('alert')).toBeInTheDocument()

    await clickLogout()

    expect(await screen.findByLabelText('Email')).toBeInTheDocument()
  })
})
