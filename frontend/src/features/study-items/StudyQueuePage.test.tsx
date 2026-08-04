import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { delay, http, HttpResponse } from 'msw'
import { server } from '../../mocks/server'
import { StudyQueuePage } from './StudyQueuePage'

describe('StudyQueuePage', () => {
  it('shows a loading state before the queue resolves', async () => {
    server.use(
      http.get('/api/study-queue', async () => {
        await delay('infinite')
      }),
    )

    render(<StudyQueuePage onSelectItem={vi.fn()} onCreateNew={vi.fn()} />)

    expect(screen.getByRole('status')).toHaveTextContent(/loading/i)
  })

  it('shows an empty state with a create action when there are no active items', async () => {
    server.use(http.get('/api/study-queue', () => HttpResponse.json([])))
    const onCreateNew = vi.fn()

    render(<StudyQueuePage onSelectItem={vi.fn()} onCreateNew={onCreateNew} />)

    expect(await screen.findByText('No active study items yet')).toBeInTheDocument()
    const [, emptyStateAction] = screen.getAllByRole('button', { name: 'New study item' })
    await userEvent.click(emptyStateAction)
    expect(onCreateNew).toHaveBeenCalled()
  })

  it('leads with the highest-ranked item and lists the rest', async () => {
    server.use(
      http.get('/api/study-queue', () =>
        HttpResponse.json([
          { id: 'a', title: 'System design: rate limiter', category: 'SystemDesign', effectiveScore: 90, priorityOverrideReason: null, lastReviewedAtUtc: null },
          { id: 'b', title: 'Two Sum', category: 'LeetCode', effectiveScore: 40, priorityOverrideReason: null, lastReviewedAtUtc: null },
        ]),
      ),
    )

    render(<StudyQueuePage onSelectItem={vi.fn()} onCreateNew={vi.fn()} />)

    expect(await screen.findByRole('heading', { name: 'System design: rate limiter' })).toBeInTheDocument()
    expect(screen.getByText('Two Sum')).toBeInTheDocument()
  })

  it('opens the lead item when its Open button is clicked', async () => {
    server.use(
      http.get('/api/study-queue', () =>
        HttpResponse.json([
          { id: 'a', title: 'System design: rate limiter', category: 'SystemDesign', effectiveScore: 90, priorityOverrideReason: null, lastReviewedAtUtc: null },
        ]),
      ),
    )
    const onSelectItem = vi.fn()

    render(<StudyQueuePage onSelectItem={onSelectItem} onCreateNew={vi.fn()} />)
    await screen.findByRole('heading', { name: 'System design: rate limiter' })
    await userEvent.click(screen.getByRole('button', { name: 'Open' }))

    expect(onSelectItem).toHaveBeenCalledWith('a')
  })

  it('shows a retryable error when loading fails (401 that a session refresh cannot recover)', async () => {
    // apiClient's own middleware auto-retries a 401 once, after a silent refresh — overriding
    // /auth/refresh to also fail is what makes this reach StudyQueuePage's error state at all,
    // instead of the middleware quietly recovering it before fetchStudyQueue ever sees a failure.
    server.use(http.post('/auth/refresh', () => new HttpResponse(null, { status: 401 })))
    let callCount = 0
    server.use(
      http.get('/api/study-queue', () => {
        callCount += 1
        return callCount === 1 ? new HttpResponse(null, { status: 401 }) : HttpResponse.json([])
      }),
    )

    render(<StudyQueuePage onSelectItem={vi.fn()} onCreateNew={vi.fn()} />)

    expect(await screen.findByRole('alert')).toHaveTextContent(/status 401/i)
    await userEvent.click(screen.getByRole('button', { name: 'Try again' }))

    expect(await screen.findByText('No active study items yet')).toBeInTheDocument()
  })

  it('shows a retryable error on a network failure', async () => {
    let callCount = 0
    server.use(
      http.get('/api/study-queue', () => {
        callCount += 1
        return callCount === 1 ? HttpResponse.error() : HttpResponse.json([])
      }),
    )

    render(<StudyQueuePage onSelectItem={vi.fn()} onCreateNew={vi.fn()} />)

    expect(await screen.findByRole('alert')).toBeInTheDocument()
    await userEvent.click(screen.getByRole('button', { name: 'Try again' }))

    expect(await screen.findByText('No active study items yet')).toBeInTheDocument()
  })
})
