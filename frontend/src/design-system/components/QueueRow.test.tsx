import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { QueueRow } from './QueueRow'

describe('QueueRow', () => {
  it('renders title, category and the API-provided EffectiveScore', () => {
    render(
      <ul>
        <QueueRow
          item={{
            id: 'item-1',
            title: 'Two Sum',
            category: 'LeetCode',
            effectiveScore: 72,
            priorityOverrideReason: null,
            lastReviewedAtUtc: null,
          }}
          onSelect={vi.fn()}
        />
      </ul>,
    )

    expect(screen.getByText('Two Sum')).toBeInTheDocument()
    expect(screen.getByText('LeetCode')).toBeInTheDocument()
    expect(screen.getByText('72')).toBeInTheDocument()
    expect(screen.getByText('Not yet reviewed')).toBeInTheDocument()
  })

  it('shows the priority override reason instead of review recency when one is set', () => {
    render(
      <ul>
        <QueueRow
          item={{
            id: 'item-1',
            title: 'Two Sum',
            category: 'LeetCode',
            effectiveScore: 95,
            priorityOverrideReason: 'Interview next week',
            lastReviewedAtUtc: '2026-01-01T00:00:00Z',
          }}
          onSelect={vi.fn()}
        />
      </ul>,
    )

    expect(screen.getByText('Interview next week')).toBeInTheDocument()
  })

  it('calls onSelect with the item id when clicked', async () => {
    const onSelect = vi.fn()
    render(
      <ul>
        <QueueRow
          item={{ id: 'item-1', title: 'Two Sum', category: 'LeetCode', effectiveScore: 72, priorityOverrideReason: null, lastReviewedAtUtc: null }}
          onSelect={onSelect}
        />
      </ul>,
    )

    await userEvent.click(screen.getByRole('button'))

    expect(onSelect).toHaveBeenCalledWith('item-1')
  })
})
