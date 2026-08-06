import { describe, it, expect } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { delay, http, HttpResponse } from 'msw'
import { server } from '../../mocks/server'
import { ProfessionalProfilePage } from './ProfessionalProfilePage'

const PROFILE = {
  id: 'profile-1',
  contactInfo: { name: 'Ada Lovelace', email: 'ada@example.com', phone: null, address: null, photoStorageKey: null },
  summaryMarkdown: 'Backend engineer.',
  experience: [],
  education: [],
  skills: [],
  languages: [],
  certifications: [],
  projects: [],
  profileLinks: [],
  createdAtUtc: '2024-01-01T00:00:00Z',
  updatedAtUtc: '2024-01-01T00:00:00Z',
}

describe('ProfessionalProfilePage', () => {
  it('shows a loading state before the profile resolves', () => {
    server.use(
      http.get('/api/professional-profile', async () => {
        await delay('infinite')
      }),
    )

    render(<ProfessionalProfilePage />)

    expect(screen.getByRole('status')).toHaveTextContent(/loading/i)
  })

  it('shows a create-profile form when no profile exists yet', async () => {
    server.use(http.get('/api/professional-profile', () => new HttpResponse(null, { status: 404 })))

    render(<ProfessionalProfilePage />)

    expect(await screen.findByRole('heading', { name: 'Create your professional profile' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Create profile' })).toBeInTheDocument()
  })

  it('creates a profile and shows the editor afterwards', async () => {
    let created = false
    server.use(
      http.get('/api/professional-profile', () => (created ? HttpResponse.json(PROFILE) : new HttpResponse(null, { status: 404 }))),
      http.post('/api/professional-profile', () => {
        created = true
        return new HttpResponse(null, { status: 201 })
      }),
    )

    render(<ProfessionalProfilePage />)
    await screen.findByRole('heading', { name: 'Create your professional profile' })

    await userEvent.type(screen.getByLabelText('Name'), 'Ada Lovelace')
    await userEvent.type(screen.getByLabelText('Email'), 'ada@example.com')
    await userEvent.type(screen.getByLabelText('Summary'), 'Backend engineer.')
    await userEvent.click(screen.getByRole('button', { name: 'Create profile' }))

    expect(await screen.findByRole('heading', { name: 'Professional profile' })).toBeInTheDocument()
  })

  it('loads and displays the existing profile', async () => {
    server.use(http.get('/api/professional-profile', () => HttpResponse.json(PROFILE)))

    render(<ProfessionalProfilePage />)

    expect(await screen.findByRole('heading', { name: 'Professional profile' })).toBeInTheDocument()
    expect(screen.getByDisplayValue('Ada Lovelace')).toBeInTheDocument()
    expect(screen.getByDisplayValue('Backend engineer.')).toBeInTheDocument()
  })

  it('shows a retryable error when the initial load fails', async () => {
    let callCount = 0
    server.use(
      http.get('/api/professional-profile', () => {
        callCount += 1
        return callCount === 1 ? HttpResponse.error() : HttpResponse.json(PROFILE)
      }),
    )

    render(<ProfessionalProfilePage />)

    expect(await screen.findByRole('alert')).toBeInTheDocument()
    await userEvent.click(screen.getByRole('button', { name: 'Try again' }))

    expect(await screen.findByRole('heading', { name: 'Professional profile' })).toBeInTheDocument()
  })

  it('switches between section tabs', async () => {
    server.use(http.get('/api/professional-profile', () => HttpResponse.json(PROFILE)))

    render(<ProfessionalProfilePage />)
    await screen.findByRole('heading', { name: 'Professional profile' })

    await userEvent.click(screen.getByRole('tab', { name: 'Experience' }))

    expect(screen.getByText('No experience entries yet.')).toBeInTheDocument()
  })
})
