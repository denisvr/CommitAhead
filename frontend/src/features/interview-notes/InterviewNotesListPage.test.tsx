import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { delay, http, HttpResponse } from 'msw'
import { server } from '../../mocks/server'
import { InterviewNotesListPage } from './InterviewNotesListPage'

const NOTE = {
  id: 'n1',
  company: 'Acme',
  role: 'Backend Engineer',
  interviewRound: 'Technical',
  sequenceNumber: 1,
  otherLabel: null,
  date: '2026-01-15',
  questions: [],
  gaps: [],
  lessons: [],
  jobAnalysisId: null,
  createdAtUtc: '2026-01-01T00:00:00Z',
  updatedAtUtc: '2026-01-01T00:00:00Z',
}

describe('InterviewNotesListPage', () => {
  it('shows a loading state before the list resolves', async () => {
    server.use(
      http.get('/api/interview-notes', async () => {
        await delay('infinite')
      }),
    )

    render(<InterviewNotesListPage onSelectNote={vi.fn()} onCreateNew={vi.fn()} />)

    expect(screen.getByRole('status')).toHaveTextContent(/loading/i)
  })

  it('shows an empty state with a call to action when there are none yet', async () => {
    server.use(http.get('/api/interview-notes', () => HttpResponse.json([])))
    const onCreateNew = vi.fn()

    render(<InterviewNotesListPage onSelectNote={vi.fn()} onCreateNew={onCreateNew} />)

    expect(await screen.findByText('No interview notes yet')).toBeInTheDocument()
    await userEvent.click(screen.getAllByRole('button', { name: 'New interview note' })[0])
    expect(onCreateNew).toHaveBeenCalled()
  })

  it('lists notes and opens one when its row is clicked', async () => {
    server.use(http.get('/api/interview-notes', () => HttpResponse.json([NOTE])))
    const onSelectNote = vi.fn()

    render(<InterviewNotesListPage onSelectNote={onSelectNote} onCreateNew={vi.fn()} />)
    await userEvent.click(await screen.findByText('Acme — Backend Engineer'))

    expect(onSelectNote).toHaveBeenCalledWith('n1')
  })

  it('shows a retryable error when loading fails', async () => {
    let callCount = 0
    server.use(
      http.get('/api/interview-notes', () => {
        callCount += 1
        return callCount === 1 ? new HttpResponse(null, { status: 500 }) : HttpResponse.json([])
      }),
    )

    render(<InterviewNotesListPage onSelectNote={vi.fn()} onCreateNew={vi.fn()} />)

    expect(await screen.findByRole('alert')).toBeInTheDocument()
    await userEvent.click(screen.getByRole('button', { name: 'Try again' }))

    expect(await screen.findByText('No interview notes yet')).toBeInTheDocument()
  })
})
