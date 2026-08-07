import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { delay, http, HttpResponse } from 'msw'
import { server } from '../../mocks/server'
import { JobAnalysesListPage } from './JobAnalysesListPage'

const ANALYSIS = {
  id: 'a1',
  title: 'Acme — Senior Backend Engineer',
  jobSource: { kind: 'PastedText', content: 'Job posting text.' },
  requirements: [],
  gaps: [],
  notesMarkdown: null,
  createdAtUtc: '2026-01-01T00:00:00Z',
  updatedAtUtc: '2026-01-01T00:00:00Z',
}

describe('JobAnalysesListPage', () => {
  it('shows a loading state before the list resolves', async () => {
    server.use(
      http.get('/api/job-analyses', async () => {
        await delay('infinite')
      }),
    )

    render(<JobAnalysesListPage onSelectAnalysis={vi.fn()} onCreateNew={vi.fn()} />)

    expect(screen.getByRole('status')).toHaveTextContent(/loading/i)
  })

  it('shows an empty state with a call to action when there are none yet', async () => {
    server.use(http.get('/api/job-analyses', () => HttpResponse.json([])))
    const onCreateNew = vi.fn()

    render(<JobAnalysesListPage onSelectAnalysis={vi.fn()} onCreateNew={onCreateNew} />)

    expect(await screen.findByText('No job analyses yet')).toBeInTheDocument()
    await userEvent.click(screen.getAllByRole('button', { name: 'New job analysis' })[0])
    expect(onCreateNew).toHaveBeenCalled()
  })

  it('lists analyses and labels the JobSource provenance', async () => {
    server.use(http.get('/api/job-analyses', () => HttpResponse.json([ANALYSIS])))

    render(<JobAnalysesListPage onSelectAnalysis={vi.fn()} onCreateNew={vi.fn()} />)

    expect(await screen.findByText('Acme — Senior Backend Engineer')).toBeInTheDocument()
    expect(screen.getByText('Pasted text')).toBeInTheDocument()
  })

  it('opens an analysis when its row is clicked', async () => {
    server.use(http.get('/api/job-analyses', () => HttpResponse.json([ANALYSIS])))
    const onSelectAnalysis = vi.fn()

    render(<JobAnalysesListPage onSelectAnalysis={onSelectAnalysis} onCreateNew={vi.fn()} />)
    await userEvent.click(await screen.findByText('Acme — Senior Backend Engineer'))

    expect(onSelectAnalysis).toHaveBeenCalledWith('a1')
  })

  it('shows a retryable error when loading fails', async () => {
    let callCount = 0
    server.use(
      http.get('/api/job-analyses', () => {
        callCount += 1
        return callCount === 1 ? new HttpResponse(null, { status: 500 }) : HttpResponse.json([])
      }),
    )

    render(<JobAnalysesListPage onSelectAnalysis={vi.fn()} onCreateNew={vi.fn()} />)

    expect(await screen.findByRole('alert')).toBeInTheDocument()
    await userEvent.click(screen.getByRole('button', { name: 'Try again' }))

    expect(await screen.findByText('No job analyses yet')).toBeInTheDocument()
  })
})
