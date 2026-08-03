import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { StudyItemsListPage } from './StudyItemsListPage'

const { fetchStudyItemsMock } = vi.hoisted(() => ({ fetchStudyItemsMock: vi.fn() }))

vi.mock('./api', () => ({
  fetchStudyItems: fetchStudyItemsMock,
}))

describe('StudyItemsListPage', () => {
  beforeEach(() => {
    fetchStudyItemsMock.mockReset()
  })

  it('shows a loading state before the list resolves', () => {
    fetchStudyItemsMock.mockReturnValue(new Promise(() => {}))

    render(<StudyItemsListPage onSelectItem={vi.fn()} onCreateNew={vi.fn()} />)

    expect(screen.getByRole('status')).toHaveTextContent(/loading/i)
  })

  it('defaults to the Active tab and loads Active items', async () => {
    fetchStudyItemsMock.mockResolvedValue([{ id: 'a', title: 'Two Sum', category: 'LeetCode', status: 'Active', importance: 3, createdAtUtc: '2026-01-01T00:00:00Z', updatedAtUtc: '2026-01-01T00:00:00Z' }])

    render(<StudyItemsListPage onSelectItem={vi.fn()} onCreateNew={vi.fn()} />)

    expect(await screen.findByText('Two Sum')).toBeInTheDocument()
    expect(fetchStudyItemsMock).toHaveBeenCalledWith('Active')
    expect(screen.getByRole('tab', { name: 'Active' })).toHaveAttribute('aria-selected', 'true')
  })

  it('switching to the Archived tab reloads with the Archived filter', async () => {
    fetchStudyItemsMock.mockResolvedValue([])

    render(<StudyItemsListPage onSelectItem={vi.fn()} onCreateNew={vi.fn()} />)
    await screen.findByText('No active study items yet')

    await userEvent.click(screen.getByRole('tab', { name: 'Archived' }))

    expect(await screen.findByText('No archived study items')).toBeInTheDocument()
    expect(fetchStudyItemsMock).toHaveBeenLastCalledWith('Archived')
  })

  it('opens an item when its row is clicked', async () => {
    fetchStudyItemsMock.mockResolvedValue([{ id: 'a', title: 'Two Sum', category: 'LeetCode', status: 'Active', importance: 3, createdAtUtc: '2026-01-01T00:00:00Z', updatedAtUtc: '2026-01-01T00:00:00Z' }])
    const onSelectItem = vi.fn()

    render(<StudyItemsListPage onSelectItem={onSelectItem} onCreateNew={vi.fn()} />)
    await userEvent.click(await screen.findByText('Two Sum'))

    expect(onSelectItem).toHaveBeenCalledWith('a')
  })

  it('shows a retryable error when loading fails', async () => {
    fetchStudyItemsMock.mockRejectedValueOnce(new Error('Network down')).mockResolvedValueOnce([])

    render(<StudyItemsListPage onSelectItem={vi.fn()} onCreateNew={vi.fn()} />)

    expect(await screen.findByRole('alert')).toHaveTextContent('Network down')
    await userEvent.click(screen.getByRole('button', { name: 'Try again' }))

    expect(await screen.findByText('No active study items yet')).toBeInTheDocument()
  })
})
