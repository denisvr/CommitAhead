import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { delay, http, HttpResponse } from 'msw'
import { server } from '../../mocks/server'
import { StudyItemsListPage } from './StudyItemsListPage'

function itemsHandler(byStatus: Record<string, unknown[]>) {
  return http.get('/api/study-items', ({ request }) => {
    const status = new URL(request.url).searchParams.get('status') ?? 'Active'
    return HttpResponse.json(byStatus[status] ?? [])
  })
}

describe('StudyItemsListPage', () => {
  it('shows a loading state before the list resolves', async () => {
    server.use(
      http.get('/api/study-items', async () => {
        await delay('infinite')
      }),
    )

    render(<StudyItemsListPage onSelectItem={vi.fn()} onCreateNew={vi.fn()} />)

    expect(screen.getByRole('status')).toHaveTextContent(/loading/i)
  })

  it('defaults to the Active tab and loads Active items', async () => {
    server.use(
      itemsHandler({
        Active: [{ id: 'a', title: 'Two Sum', category: 'LeetCode', status: 'Active', importance: 3, createdAtUtc: '2026-01-01T00:00:00Z', updatedAtUtc: '2026-01-01T00:00:00Z' }],
      }),
    )

    render(<StudyItemsListPage onSelectItem={vi.fn()} onCreateNew={vi.fn()} />)

    expect(await screen.findByText('Two Sum')).toBeInTheDocument()
    expect(screen.getByRole('tab', { name: 'Active' })).toHaveAttribute('aria-selected', 'true')
  })

  it('switching to the Archived tab reloads with the Archived filter', async () => {
    server.use(
      itemsHandler({
        Active: [],
        Archived: [{ id: 'b', title: 'Old problem', category: 'LeetCode', status: 'Archived', importance: 2, createdAtUtc: '2026-01-01T00:00:00Z', updatedAtUtc: '2026-01-01T00:00:00Z' }],
      }),
    )

    render(<StudyItemsListPage onSelectItem={vi.fn()} onCreateNew={vi.fn()} />)
    await screen.findByText('No active study items yet')

    await userEvent.click(screen.getByRole('tab', { name: 'Archived' }))

    expect(await screen.findByText('Old problem')).toBeInTheDocument()
  })

  it('opens an item when its row is clicked', async () => {
    server.use(
      itemsHandler({
        Active: [{ id: 'a', title: 'Two Sum', category: 'LeetCode', status: 'Active', importance: 3, createdAtUtc: '2026-01-01T00:00:00Z', updatedAtUtc: '2026-01-01T00:00:00Z' }],
      }),
    )
    const onSelectItem = vi.fn()

    render(<StudyItemsListPage onSelectItem={onSelectItem} onCreateNew={vi.fn()} />)
    await userEvent.click(await screen.findByText('Two Sum'))

    expect(onSelectItem).toHaveBeenCalledWith('a')
  })

  it('shows a retryable error when loading fails (server error)', async () => {
    let callCount = 0
    server.use(
      http.get('/api/study-items', () => {
        callCount += 1
        return callCount === 1 ? new HttpResponse(null, { status: 500 }) : HttpResponse.json([])
      }),
    )

    render(<StudyItemsListPage onSelectItem={vi.fn()} onCreateNew={vi.fn()} />)

    expect(await screen.findByRole('alert')).toBeInTheDocument()
    await userEvent.click(screen.getByRole('button', { name: 'Try again' }))

    expect(await screen.findByText('No active study items yet')).toBeInTheDocument()
  })
})
