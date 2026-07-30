import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { StudyQueuePage } from './StudyQueuePage'

const { fetchStudyQueueMock } = vi.hoisted(() => ({ fetchStudyQueueMock: vi.fn() }))

vi.mock('./api', () => ({
  fetchStudyQueue: fetchStudyQueueMock,
}))

describe('StudyQueuePage', () => {
  beforeEach(() => {
    fetchStudyQueueMock.mockReset()
  })

  it('shows a loading state before the queue resolves', () => {
    fetchStudyQueueMock.mockReturnValue(new Promise(() => {}))

    render(<StudyQueuePage onSelectItem={vi.fn()} onCreateNew={vi.fn()} />)

    expect(screen.getByRole('status')).toHaveTextContent(/loading/i)
  })

  it('shows an empty state with a create action when there are no active items', async () => {
    fetchStudyQueueMock.mockResolvedValue([])
    const onCreateNew = vi.fn()

    render(<StudyQueuePage onSelectItem={vi.fn()} onCreateNew={onCreateNew} />)

    expect(await screen.findByText('No active study items yet')).toBeInTheDocument()
    const [, emptyStateAction] = screen.getAllByRole('button', { name: 'New study item' })
    await userEvent.click(emptyStateAction)
    expect(onCreateNew).toHaveBeenCalled()
  })

  it('leads with the highest-ranked item and lists the rest', async () => {
    fetchStudyQueueMock.mockResolvedValue([
      { id: 'a', title: 'System design: rate limiter', category: 'SystemDesign', effectiveScore: 90, priorityOverrideReason: null, lastReviewedAtUtc: null },
      { id: 'b', title: 'Two Sum', category: 'LeetCode', effectiveScore: 40, priorityOverrideReason: null, lastReviewedAtUtc: null },
    ])

    render(<StudyQueuePage onSelectItem={vi.fn()} onCreateNew={vi.fn()} />)

    expect(await screen.findByRole('heading', { name: 'System design: rate limiter' })).toBeInTheDocument()
    expect(screen.getByText('Two Sum')).toBeInTheDocument()
  })

  it('opens the lead item when its Open button is clicked', async () => {
    fetchStudyQueueMock.mockResolvedValue([
      { id: 'a', title: 'System design: rate limiter', category: 'SystemDesign', effectiveScore: 90, priorityOverrideReason: null, lastReviewedAtUtc: null },
    ])
    const onSelectItem = vi.fn()

    render(<StudyQueuePage onSelectItem={onSelectItem} onCreateNew={vi.fn()} />)
    await screen.findByRole('heading', { name: 'System design: rate limiter' })
    await userEvent.click(screen.getByRole('button', { name: 'Open' }))

    expect(onSelectItem).toHaveBeenCalledWith('a')
  })

  it('shows a retryable error when loading fails', async () => {
    fetchStudyQueueMock.mockRejectedValueOnce(new Error('Network down')).mockResolvedValueOnce([])

    render(<StudyQueuePage onSelectItem={vi.fn()} onCreateNew={vi.fn()} />)

    expect(await screen.findByRole('alert')).toHaveTextContent('Network down')
    await userEvent.click(screen.getByRole('button', { name: 'Try again' }))

    expect(await screen.findByText('No active study items yet')).toBeInTheDocument()
  })
})
