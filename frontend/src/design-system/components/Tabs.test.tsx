import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { Tabs } from './Tabs'

const TABS = [
  { key: 'Active', label: 'Active' },
  { key: 'Archived', label: 'Archived' },
]

describe('Tabs', () => {
  it('marks the active tab as selected and the rest as not selected', () => {
    render(<Tabs tabs={TABS} activeTab="Active" onChange={vi.fn()} aria-label="Filter by status" />)

    expect(screen.getByRole('tab', { name: 'Active' })).toHaveAttribute('aria-selected', 'true')
    expect(screen.getByRole('tab', { name: 'Archived' })).toHaveAttribute('aria-selected', 'false')
  })

  it('calls onChange with the clicked tab key', async () => {
    const onChange = vi.fn()
    render(<Tabs tabs={TABS} activeTab="Active" onChange={onChange} aria-label="Filter by status" />)

    await userEvent.click(screen.getByRole('tab', { name: 'Archived' }))

    expect(onChange).toHaveBeenCalledWith('Archived')
  })

  it('moves selection with the arrow keys', async () => {
    const onChange = vi.fn()
    render(<Tabs tabs={TABS} activeTab="Active" onChange={onChange} aria-label="Filter by status" />)

    screen.getByRole('tab', { name: 'Active' }).focus()
    await userEvent.keyboard('{ArrowRight}')

    expect(onChange).toHaveBeenCalledWith('Archived')
  })
})
