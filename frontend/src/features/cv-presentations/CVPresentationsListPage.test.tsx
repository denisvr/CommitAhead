import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { delay, http, HttpResponse } from 'msw'
import { server } from '../../mocks/server'
import { CVPresentationsListPage } from './CVPresentationsListPage'

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

describe('CVPresentationsListPage', () => {
  it('shows a loading state before the list resolves', () => {
    server.use(
      http.get('/api/cv-presentations', async () => {
        await delay('infinite')
      }),
    )

    render(<CVPresentationsListPage onSelectPresentation={vi.fn()} onCreateNew={vi.fn()} />)

    expect(screen.getByRole('status')).toHaveTextContent(/loading/i)
  })

  it('shows an empty state with a create action when there are no presentations', async () => {
    server.use(http.get('/api/cv-presentations', () => HttpResponse.json([])))
    const onCreateNew = vi.fn()

    render(<CVPresentationsListPage onSelectPresentation={vi.fn()} onCreateNew={onCreateNew} />)

    expect(await screen.findByText('No CV presentations yet')).toBeInTheDocument()
    await userEvent.click(screen.getAllByRole('button', { name: 'New CV presentation' })[0])
    expect(onCreateNew).toHaveBeenCalled()
  })

  it('lists existing presentations and selecting one calls onSelectPresentation', async () => {
    server.use(http.get('/api/cv-presentations', () => HttpResponse.json([PRESENTATION])))
    const onSelectPresentation = vi.fn()

    render(<CVPresentationsListPage onSelectPresentation={onSelectPresentation} onCreateNew={vi.fn()} />)

    const row = await screen.findByRole('button', { name: /UK — Senior Backend Engineer/ })
    await userEvent.click(row)

    expect(onSelectPresentation).toHaveBeenCalledWith('presentation-1')
  })

  it('shows a retryable error when the initial load fails', async () => {
    let callCount = 0
    server.use(
      http.get('/api/cv-presentations', () => {
        callCount += 1
        return callCount === 1 ? HttpResponse.error() : HttpResponse.json([])
      }),
    )

    render(<CVPresentationsListPage onSelectPresentation={vi.fn()} onCreateNew={vi.fn()} />)

    expect(await screen.findByRole('alert')).toBeInTheDocument()
    await userEvent.click(screen.getByRole('button', { name: 'Try again' }))

    expect(await screen.findByText('No CV presentations yet')).toBeInTheDocument()
  })
})
