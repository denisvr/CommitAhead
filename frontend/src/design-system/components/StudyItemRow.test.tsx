import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { StudyItemRow } from './StudyItemRow'

describe('StudyItemRow', () => {
  it('renders title, category and status', () => {
    render(
      <ul>
        <StudyItemRow item={{ id: 'item-1', title: 'Two Sum', category: 'LeetCode', status: 'Active', updatedAtUtc: '2026-01-01T00:00:00Z' }} onSelect={vi.fn()} />
      </ul>,
    )

    expect(screen.getByText('Two Sum')).toBeInTheDocument()
    expect(screen.getByText('LeetCode')).toBeInTheDocument()
    expect(screen.getByText('Active')).toBeInTheDocument()
  })

  it('calls onSelect with the item id when clicked', async () => {
    const onSelect = vi.fn()
    render(
      <ul>
        <StudyItemRow item={{ id: 'item-1', title: 'Two Sum', category: 'LeetCode', status: 'Archived', updatedAtUtc: '2026-01-01T00:00:00Z' }} onSelect={onSelect} />
      </ul>,
    )

    await userEvent.click(screen.getByRole('button'))

    expect(onSelect).toHaveBeenCalledWith('item-1')
  })
})
