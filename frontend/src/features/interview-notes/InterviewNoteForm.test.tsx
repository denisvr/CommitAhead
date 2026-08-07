import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { server } from '../../mocks/server'
import { InterviewNoteForm } from './InterviewNoteForm'
import type { InterviewNoteResponse } from './api'

const NOTE: InterviewNoteResponse = {
  id: 'n1',
  company: 'Acme',
  role: 'Backend Engineer',
  interviewRound: 'Technical',
  sequenceNumber: 1,
  otherLabel: null,
  date: '2026-01-15',
  questions: ['Tell me about a distributed system you built.'],
  gaps: [],
  lessons: [],
  jobAnalysisId: null,
  createdAtUtc: '2026-01-01T00:00:00Z',
  updatedAtUtc: '2026-01-01T00:00:00Z',
}

describe('InterviewNoteForm — create', () => {
  it('creates a note with a recorded question and reports the new id', async () => {
    let requestBody: Record<string, unknown> | undefined
    server.use(
      http.post('/api/interview-notes', async ({ request }) => {
        requestBody = (await request.json()) as Record<string, unknown>
        return HttpResponse.json({ id: 'new-note-id' }, { status: 201 })
      }),
    )
    const onCreated = vi.fn()

    render(<InterviewNoteForm mode="create" onCreated={onCreated} onCancel={vi.fn()} />)
    await userEvent.type(screen.getByLabelText('Company'), 'Acme')
    await userEvent.type(screen.getByLabelText('Role'), 'Backend Engineer')
    await userEvent.click(screen.getByRole('button', { name: 'Add question' }))
    await userEvent.type(screen.getByLabelText('Questions asked entry 1'), 'Tell me about a project you led.')
    await userEvent.click(screen.getByRole('button', { name: 'Create' }))

    expect(onCreated).toHaveBeenCalledWith('new-note-id')
    expect(requestBody?.company).toBe('Acme')
    expect(requestBody?.questions).toEqual(['Tell me about a project you led.'])
  })

  it('requires an other label once the round is switched to Other', async () => {
    render(<InterviewNoteForm mode="create" onCreated={vi.fn()} onCancel={vi.fn()} />)

    await userEvent.selectOptions(screen.getByLabelText('Round'), 'Other')

    expect(screen.getByLabelText('Other label')).toBeRequired()
  })
})

describe('InterviewNoteForm — edit', () => {
  it('pre-fills from the existing note and saves changes', async () => {
    let requestBody: Record<string, unknown> | undefined
    server.use(
      http.put('/api/interview-notes/:id', async ({ request }) => {
        requestBody = (await request.json()) as Record<string, unknown>
        return new HttpResponse(null, { status: 204 })
      }),
    )
    const onSaved = vi.fn()

    render(<InterviewNoteForm mode="edit" note={NOTE} onSaved={onSaved} onCancel={vi.fn()} />)

    expect(screen.getByLabelText('Company')).toHaveValue('Acme')
    expect(screen.getByLabelText('Questions asked entry 1')).toHaveValue('Tell me about a distributed system you built.')

    await userEvent.clear(screen.getByLabelText('Company'))
    await userEvent.type(screen.getByLabelText('Company'), 'New Co')
    await userEvent.click(screen.getByRole('button', { name: 'Save' }))

    expect(onSaved).toHaveBeenCalled()
    expect(requestBody?.company).toBe('New Co')
  })
})
