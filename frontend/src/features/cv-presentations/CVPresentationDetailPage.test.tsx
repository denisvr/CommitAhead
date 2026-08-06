import { describe, it, expect, vi } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { server } from '../../mocks/server'
import { CVPresentationDetailPage } from './CVPresentationDetailPage'

const PROFILE = {
  id: 'profile-1',
  contactInfo: { name: 'Ada Lovelace', email: 'ada@example.com', phone: null, address: null, photoStorageKey: null },
  summaryMarkdown: 'Backend engineer.',
  experience: [
    {
      id: 'exp-1',
      company: 'Acme',
      client: null,
      role: 'Engineer',
      employmentType: 'Permanent',
      startDate: { year: 2020, month: 1 },
      endDate: null,
      location: null,
      workMode: 'Remote',
      summaryMarkdown: 'Did stuff.',
      achievements: [],
      skillIds: [],
    },
  ],
  education: [{ id: 'edu-1', institution: 'MIT', degree: 'BSc', field: null, startDate: null, endDate: null, location: null, detailsMarkdown: null }],
  skills: [],
  languages: [],
  certifications: [],
  projects: [],
  profileLinks: [],
  createdAtUtc: '2024-01-01T00:00:00Z',
  updatedAtUtc: '2024-01-01T00:00:00Z',
}

const PRESENTATION = {
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

function mockLoadHandlers() {
  server.use(http.get('/api/cv-presentations/:id', () => HttpResponse.json(PRESENTATION)), http.get('/api/professional-profile', () => HttpResponse.json(PROFILE)))
}

describe('CVPresentationDetailPage selection saves', () => {
  it('disables a selection section while its own save is pending, preventing a second change from firing', async () => {
    mockLoadHandlers()
    let resolvePut: (() => void) | undefined
    const pending = new Promise<void>((resolve) => {
      resolvePut = resolve
    })
    let putCallCount = 0
    server.use(
      http.put('/api/cv-presentations/:id/experience-selections', async () => {
        putCallCount += 1
        await pending
        return new HttpResponse(null, { status: 204 })
      }),
    )

    render(<CVPresentationDetailPage presentationId="presentation-1" onBack={vi.fn()} onDeleted={vi.fn()} />)
    await screen.findByRole('heading', { name: 'UK — Senior Backend Engineer' })

    const select = screen.getByLabelText('Add experience entry')
    await userEvent.selectOptions(select, 'exp-1')

    expect(select).toBeDisabled()
    expect(putCallCount).toBe(1)

    resolvePut!()

    // "Add experience entry" only had one candidate — once it's selected, the add control has
    // nothing left to offer and stops rendering entirely. Its disappearance (rather than just
    // `getByText('Engineer — Acme')`, which would already match the still-available option's
    // text even before resolving) is what actually proves the selection was applied.
    await waitFor(() => expect(screen.queryByLabelText('Add experience entry')).not.toBeInTheDocument())
    expect(screen.getByText('Engineer — Acme')).toBeInTheDocument()
  })

  it('completing a faster section save does not overwrite a slower section that is still saving', async () => {
    mockLoadHandlers()
    let resolveExperiencePut: (() => void) | undefined
    const experiencePending = new Promise<void>((resolve) => {
      resolveExperiencePut = resolve
    })
    server.use(
      http.put('/api/cv-presentations/:id/experience-selections', async () => {
        await experiencePending
        return new HttpResponse(null, { status: 204 })
      }),
      http.put('/api/cv-presentations/:id/education-selections', () => new HttpResponse(null, { status: 204 })),
    )

    render(<CVPresentationDetailPage presentationId="presentation-1" onBack={vi.fn()} onDeleted={vi.fn()} />)
    await screen.findByRole('heading', { name: 'UK — Senior Backend Engineer' })

    // Start the slower Experience save — it stays pending until resolveExperiencePut() is called.
    await userEvent.selectOptions(screen.getByLabelText('Add experience entry'), 'exp-1')

    // The faster Education save starts (and finishes) while Experience is still in flight. Each
    // section has exactly one candidate, so once it's genuinely selected its "Add ... entry"
    // control has nothing left to offer and stops rendering — that disappearance is the
    // unambiguous signal, since `getByText('BSc — MIT')` alone would already match the
    // still-unselected option's text inside the dropdown.
    await userEvent.selectOptions(screen.getByLabelText('Add education entry'), 'edu-1')
    await waitFor(() => expect(screen.queryByLabelText('Add education entry')).not.toBeInTheDocument())

    // Now let the slower Experience save complete.
    resolveExperiencePut!()
    await waitFor(() => expect(screen.queryByLabelText('Add experience entry')).not.toBeInTheDocument())

    // Both selections must still be applied — before the functional-setState fix, Experience's
    // onSaved closed over a stale `presentation` snapshot (from before Education's update landed)
    // and would have reverted educationSelections back to empty when it finally applied its own
    // change, which would bring "Add education entry" (and its one candidate) back.
    expect(screen.queryByLabelText('Add education entry')).not.toBeInTheDocument()
    expect(screen.getByText('BSc — MIT')).toBeInTheDocument()
    expect(screen.getByText('Engineer — Acme')).toBeInTheDocument()
  })
})

describe('CVPresentationDetailPage load states', () => {
  it('shows a loading state before the presentation and profile resolve', () => {
    server.use(http.get('/api/cv-presentations/:id', () => new Promise(() => {})), http.get('/api/professional-profile', () => new Promise(() => {})))

    render(<CVPresentationDetailPage presentationId="presentation-1" onBack={vi.fn()} onDeleted={vi.fn()} />)

    expect(screen.getByRole('status')).toHaveTextContent(/loading/i)
  })

  it('shows a not-found message when the presentation does not exist', async () => {
    server.use(http.get('/api/cv-presentations/:id', () => new HttpResponse(null, { status: 404 })), http.get('/api/professional-profile', () => HttpResponse.json(PROFILE)))
    const onBack = vi.fn()

    render(<CVPresentationDetailPage presentationId="missing" onBack={onBack} onDeleted={vi.fn()} />)

    await userEvent.click(await screen.findByRole('button', { name: 'Back to CV presentations' }))
    expect(onBack).toHaveBeenCalled()
  })

  it('shows a retryable error when the initial load fails', async () => {
    let callCount = 0
    server.use(
      http.get('/api/cv-presentations/:id', () => {
        callCount += 1
        return callCount === 1 ? HttpResponse.error() : HttpResponse.json(PRESENTATION)
      }),
      http.get('/api/professional-profile', () => HttpResponse.json(PROFILE)),
    )

    render(<CVPresentationDetailPage presentationId="presentation-1" onBack={vi.fn()} onDeleted={vi.fn()} />)

    expect(await screen.findByRole('alert')).toBeInTheDocument()
    await userEvent.click(screen.getByRole('button', { name: 'Try again' }))

    expect(await screen.findByRole('heading', { name: 'UK — Senior Backend Engineer' })).toBeInTheDocument()
  })
})

describe('CVPresentationDetailPage delete', () => {
  it('asks for confirmation, then deletes and calls onDeleted', async () => {
    mockLoadHandlers()
    let deleteCalled = false
    server.use(http.delete('/api/cv-presentations/:id', () => {
      deleteCalled = true
      return new HttpResponse(null, { status: 204 })
    }))
    const onDeleted = vi.fn()

    render(<CVPresentationDetailPage presentationId="presentation-1" onBack={vi.fn()} onDeleted={onDeleted} />)
    await screen.findByRole('heading', { name: 'UK — Senior Backend Engineer' })

    await userEvent.click(screen.getByRole('button', { name: /delete/i }))
    expect(screen.getByText('Delete this CV presentation permanently?')).toBeInTheDocument()
    expect(deleteCalled).toBe(false)

    await userEvent.click(screen.getByRole('button', { name: 'Yes, delete' }))

    expect(deleteCalled).toBe(true)
    expect(onDeleted).toHaveBeenCalled()
  })

  it('shows an error and does not call onDeleted when deletion fails', async () => {
    mockLoadHandlers()
    server.use(http.delete('/api/cv-presentations/:id', () => HttpResponse.error()))
    const onDeleted = vi.fn()

    render(<CVPresentationDetailPage presentationId="presentation-1" onBack={vi.fn()} onDeleted={onDeleted} />)
    await screen.findByRole('heading', { name: 'UK — Senior Backend Engineer' })

    await userEvent.click(screen.getByRole('button', { name: /delete/i }))
    await userEvent.click(screen.getByRole('button', { name: 'Yes, delete' }))

    expect(await screen.findByRole('alert')).toBeInTheDocument()
    expect(onDeleted).not.toHaveBeenCalled()
  })
})

describe('CVPresentationDetailPage selection reordering', () => {
  const PROFILE_WITH_TWO_ENTRIES = {
    ...PROFILE,
    experience: [
      ...PROFILE.experience,
      {
        id: 'exp-2',
        company: 'Globex',
        client: null,
        role: 'Senior Engineer',
        employmentType: 'Permanent',
        startDate: { year: 2021, month: 1 },
        endDate: null,
        location: null,
        workMode: 'Remote',
        summaryMarkdown: 'Second role.',
        achievements: [],
        skillIds: [],
      },
    ],
  }
  const PRESENTATION_WITH_SELECTIONS = { ...PRESENTATION, experienceSelections: ['exp-1', 'exp-2'] }

  it('moving a selected entry down reorders it and saves the new order', async () => {
    let savedOrder: unknown
    server.use(
      http.get('/api/cv-presentations/:id', () => HttpResponse.json(PRESENTATION_WITH_SELECTIONS)),
      http.get('/api/professional-profile', () => HttpResponse.json(PROFILE_WITH_TWO_ENTRIES)),
      http.put('/api/cv-presentations/:id/experience-selections', async ({ request }) => {
        savedOrder = await request.json()
        return new HttpResponse(null, { status: 204 })
      }),
    )

    render(<CVPresentationDetailPage presentationId="presentation-1" onBack={vi.fn()} onDeleted={vi.fn()} />)
    await screen.findByRole('heading', { name: 'UK — Senior Backend Engineer' })

    await userEvent.click(screen.getByRole('button', { name: 'Move Engineer — Acme down' }))

    await waitFor(() => expect(savedOrder).toEqual(['exp-2', 'exp-1']))
  })
})
