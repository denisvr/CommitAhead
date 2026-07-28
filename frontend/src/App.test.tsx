import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import App from './App'

const { getMock } = vi.hoisted(() => ({ getMock: vi.fn() }))

vi.mock('./api/client', () => ({
  apiClient: { GET: getMock },
}))

describe('App', () => {
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
})
