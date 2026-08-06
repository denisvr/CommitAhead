import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { server } from '../../mocks/server'
import { CVPresentationForm } from './CVPresentationForm'
import type { CVPresentationResponse } from './api'

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

const PRESENTATION: CVPresentationResponse = {
  id: 'presentation-1',
  professionalProfileId: 'profile-1',
  label: 'UK — Senior Backend Engineer',
  targetMarket: 'United Kingdom',
  targetRole: 'Senior Backend Engineer',
  locale: 'en-GB',
  templateKey: 'modern-one-page',
  summaryOverrideMarkdown: null,
  includePhoto: false,
  includeEmail: true,
  includePhone: true,
  includeAddress: false,
  dateFormat: 'dd MMM yyyy',
  pageLimit: 2,
  experienceSelections: [],
  educationSelections: [],
  skillSelections: [],
  languageSelections: [],
  certificationSelections: [],
  projectSelections: [],
  profileLinkSelections: [],
  createdAtUtc: '2024-01-01T00:00:00Z',
  updatedAtUtc: '2024-01-01T00:00:00Z',
}

async function fillRequiredFields() {
  await userEvent.type(screen.getByLabelText('Label'), 'US — Backend Engineer')
  await userEvent.type(screen.getByLabelText('Target market'), 'United States')
}

describe('CVPresentationForm — create', () => {
  it('shows a message and a way back to the profile when no professional profile exists yet', async () => {
    server.use(http.get('/api/professional-profile', () => new HttpResponse(null, { status: 404 })))
    const onGoToProfile = vi.fn()

    render(<CVPresentationForm mode="create" onCreated={vi.fn()} onCancel={vi.fn()} onGoToProfile={onGoToProfile} />)

    expect(await screen.findByText(/need a professional profile/i)).toBeInTheDocument()
    await userEvent.click(screen.getByRole('button', { name: 'Go to your professional profile' }))
    expect(onGoToProfile).toHaveBeenCalled()
  })

  it('creates a presentation against the current professional profile and reports the new id', async () => {
    server.use(http.get('/api/professional-profile', () => HttpResponse.json(PROFILE)))
    let requestBody: Record<string, unknown> | undefined
    server.use(
      http.post('/api/cv-presentations', async ({ request }) => {
        requestBody = (await request.json()) as Record<string, unknown>
        return HttpResponse.json({ id: 'new-presentation-id' }, { status: 201 })
      }),
    )
    const onCreated = vi.fn()

    render(<CVPresentationForm mode="create" onCreated={onCreated} onCancel={vi.fn()} onGoToProfile={vi.fn()} />)
    await screen.findByLabelText('Label')
    await fillRequiredFields()

    await userEvent.click(screen.getByRole('button', { name: 'Create' }))

    expect(onCreated).toHaveBeenCalledWith('new-presentation-id')
    expect(requestBody?.professionalProfileId).toBe('profile-1')
    expect(requestBody?.label).toBe('US — Backend Engineer')
  })

  it('shows a server-side rejection instead of crashing when creation is refused', async () => {
    server.use(http.get('/api/professional-profile', () => HttpResponse.json(PROFILE)))
    server.use(http.post('/api/cv-presentations', () => new HttpResponse(null, { status: 422 })))

    render(<CVPresentationForm mode="create" onCreated={vi.fn()} onCancel={vi.fn()} onGoToProfile={vi.fn()} />)
    await screen.findByLabelText('Label')
    await fillRequiredFields()

    await userEvent.click(screen.getByRole('button', { name: 'Create' }))

    expect(await screen.findByRole('alert')).toBeInTheDocument()
  })
})

describe('CVPresentationForm — edit', () => {
  it('pre-fills from the existing presentation and saves changes', async () => {
    let requestBody: Record<string, unknown> | undefined
    server.use(
      http.put('/api/cv-presentations/:id', async ({ request }) => {
        requestBody = (await request.json()) as Record<string, unknown>
        return new HttpResponse(null, { status: 204 })
      }),
    )
    const onSaved = vi.fn()

    render(<CVPresentationForm mode="edit" presentation={PRESENTATION} onSaved={onSaved} onCancel={vi.fn()} />)

    expect(screen.getByLabelText('Label')).toHaveValue('UK — Senior Backend Engineer')

    await userEvent.clear(screen.getByLabelText('Label'))
    await userEvent.type(screen.getByLabelText('Label'), 'Germany — Backend Engineer')
    await userEvent.click(screen.getByRole('button', { name: 'Save' }))

    expect(onSaved).toHaveBeenCalled()
    expect(requestBody?.label).toBe('Germany — Backend Engineer')
  })

  it('shows an error and does not call onSaved when the update fails', async () => {
    server.use(http.put('/api/cv-presentations/:id', () => HttpResponse.error()))
    const onSaved = vi.fn()

    render(<CVPresentationForm mode="edit" presentation={PRESENTATION} onSaved={onSaved} onCancel={vi.fn()} />)

    await userEvent.click(screen.getByRole('button', { name: 'Save' }))

    expect(await screen.findByRole('alert')).toBeInTheDocument()
    expect(onSaved).not.toHaveBeenCalled()
  })
})
