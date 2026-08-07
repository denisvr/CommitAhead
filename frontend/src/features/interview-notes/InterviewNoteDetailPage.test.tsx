import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { server } from '../../mocks/server'
import { InterviewNoteDetailPage } from './InterviewNoteDetailPage'

const NOTE = {
  id: 'n1',
  company: 'Acme',
  role: 'Backend Engineer',
  interviewRound: 'Technical',
  sequenceNumber: 1,
  otherLabel: null,
  date: '2026-01-15',
  questions: ['Tell me about a distributed system you built.'],
  gaps: ['Limited exposure to Kubernetes.'],
  lessons: ['Prepare a concrete scaling story next time.'],
  jobAnalysisId: null,
  createdAtUtc: '2026-01-01T00:00:00Z',
  updatedAtUtc: '2026-01-01T00:00:00Z',
}

describe('InterviewNoteDetailPage', () => {
  it('shows a not-found message for a missing note', async () => {
    server.use(http.get('/api/interview-notes/:id', () => new HttpResponse(null, { status: 404 })))

    render(<InterviewNoteDetailPage noteId="missing" onBack={vi.fn()} onDeleted={vi.fn()} />)

    expect(await screen.findByText('This interview note could not be found.')).toBeInTheDocument()
  })

  it('shows the recorded questions, gaps, and lessons', async () => {
    server.use(http.get('/api/interview-notes/:id', () => HttpResponse.json(NOTE)))

    render(<InterviewNoteDetailPage noteId="n1" onBack={vi.fn()} onDeleted={vi.fn()} />)

    expect(await screen.findByText('Tell me about a distributed system you built.')).toBeInTheDocument()
    expect(screen.getByText('Limited exposure to Kubernetes.')).toBeInTheDocument()
    expect(screen.getByText('Prepare a concrete scaling story next time.')).toBeInTheDocument()
  })

  it('deletes the note after confirmation', async () => {
    server.use(http.get('/api/interview-notes/:id', () => HttpResponse.json(NOTE)))
    server.use(http.delete('/api/interview-notes/:id', () => new HttpResponse(null, { status: 204 })))
    const onDeleted = vi.fn()

    render(<InterviewNoteDetailPage noteId="n1" onBack={vi.fn()} onDeleted={onDeleted} />)
    await userEvent.click(await screen.findByRole('button', { name: /delete/i }))
    await userEvent.click(screen.getByRole('button', { name: 'Yes, delete' }))

    expect(onDeleted).toHaveBeenCalled()
  })
})
