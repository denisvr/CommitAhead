import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { server } from '../../mocks/server'
import { JobAnalysisDetailPage } from './JobAnalysisDetailPage'

const UPLOADED_ANALYSIS = {
  id: 'a1',
  title: 'Acme — Senior Backend Engineer',
  jobSource: { kind: 'UploadedFile', originalFileName: 'posting.pdf', mimeType: 'application/pdf', extractedText: 'We need a backend engineer.' },
  requirements: [],
  gaps: [],
  notesMarkdown: null,
  createdAtUtc: '2026-01-01T00:00:00Z',
  updatedAtUtc: '2026-01-01T00:00:00Z',
}

describe('JobAnalysisDetailPage', () => {
  it('shows a not-found message for a missing analysis', async () => {
    server.use(http.get('/api/job-analyses/:id', () => new HttpResponse(null, { status: 404 })))

    render(<JobAnalysisDetailPage analysisId="missing" onBack={vi.fn()} onDeleted={vi.fn()} onAnalyzed={vi.fn()} />)

    expect(await screen.findByText('This job analysis could not be found.')).toBeInTheDocument()
  })

  it('shows the extracted text for an uploaded PDF, for verification', async () => {
    server.use(http.get('/api/job-analyses/:id', () => HttpResponse.json(UPLOADED_ANALYSIS)))

    render(<JobAnalysisDetailPage analysisId="a1" onBack={vi.fn()} onDeleted={vi.fn()} onAnalyzed={vi.fn()} />)

    expect(await screen.findByText('We need a backend engineer.')).toBeInTheDocument()
    expect(screen.getByText('Uploaded PDF — posting.pdf')).toBeInTheDocument()
  })

  it('saves title/notes edits', async () => {
    server.use(http.get('/api/job-analyses/:id', () => HttpResponse.json(UPLOADED_ANALYSIS)))
    let requestBody: Record<string, unknown> | undefined
    server.use(
      http.put('/api/job-analyses/:id', async ({ request }) => {
        requestBody = (await request.json()) as Record<string, unknown>
        return new HttpResponse(null, { status: 204 })
      }),
    )

    render(<JobAnalysisDetailPage analysisId="a1" onBack={vi.fn()} onDeleted={vi.fn()} onAnalyzed={vi.fn()} />)
    await userEvent.click(await screen.findByRole('button', { name: /edit/i }))
    await userEvent.clear(screen.getByLabelText('Title'))
    await userEvent.type(screen.getByLabelText('Title'), 'New title')
    await userEvent.click(screen.getByRole('button', { name: 'Save' }))

    expect(requestBody?.title).toBe('New title')
  })

  it('starts an analysis and hands the returned draft id to onAnalyzed', async () => {
    server.use(http.get('/api/job-analyses/:id', () => HttpResponse.json(UPLOADED_ANALYSIS)))
    server.use(http.post('/api/job-analyses/:id/analyze', () => HttpResponse.json({ outcome: 'Created', analysisDraftId: 'draft-1' }, { status: 201 })))
    const onAnalyzed = vi.fn()

    render(<JobAnalysisDetailPage analysisId="a1" onBack={vi.fn()} onDeleted={vi.fn()} onAnalyzed={onAnalyzed} />)
    await userEvent.click(await screen.findByRole('button', { name: 'Analyze' }))

    expect(onAnalyzed).toHaveBeenCalledWith('draft-1')
  })

  it('shows an inline message instead of navigating when a draft is already pending', async () => {
    server.use(http.get('/api/job-analyses/:id', () => HttpResponse.json(UPLOADED_ANALYSIS)))
    server.use(
      http.post('/api/job-analyses/:id/analyze', () =>
        HttpResponse.json({ title: 'Conflict', status: 409, extensions: { outcomeCode: 'DraftAlreadyPending' } }, { status: 409 }),
      ),
    )
    const onAnalyzed = vi.fn()

    render(<JobAnalysisDetailPage analysisId="a1" onBack={vi.fn()} onDeleted={vi.fn()} onAnalyzed={onAnalyzed} />)
    await userEvent.click(await screen.findByRole('button', { name: 'Analyze' }))

    expect(await screen.findByText(/already pending review/i)).toBeInTheDocument()
    expect(onAnalyzed).not.toHaveBeenCalled()
  })

  it('deletes the analysis after confirmation', async () => {
    server.use(http.get('/api/job-analyses/:id', () => HttpResponse.json(UPLOADED_ANALYSIS)))
    server.use(http.delete('/api/job-analyses/:id', () => new HttpResponse(null, { status: 204 })))
    const onDeleted = vi.fn()

    render(<JobAnalysisDetailPage analysisId="a1" onBack={vi.fn()} onDeleted={onDeleted} onAnalyzed={vi.fn()} />)
    await userEvent.click(await screen.findByRole('button', { name: /delete/i }))
    await userEvent.click(screen.getByRole('button', { name: 'Yes, delete' }))

    expect(onDeleted).toHaveBeenCalled()
  })
})
