import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { SelectionOrderEditor } from './SelectionOrderEditor'

type Candidate = { id: string; label: string }

const CANDIDATES: Candidate[] = [
  { id: 'a', label: 'Alpha' },
  { id: 'b', label: 'Beta' },
  { id: 'c', label: 'Gamma' },
]

describe('SelectionOrderEditor', () => {
  it('shows the empty label when nothing is selected', () => {
    render(
      <SelectionOrderEditor
        candidates={CANDIDATES}
        selectedIds={[]}
        onChange={vi.fn()}
        getId={(c) => c.id}
        getLabel={(c) => c.label}
        addLabel="Add entry"
        emptyLabel="Nothing selected yet."
      />,
    )

    expect(screen.getByText('Nothing selected yet.')).toBeInTheDocument()
  })

  it('lists only candidates not yet selected in the add dropdown', () => {
    render(
      <SelectionOrderEditor
        candidates={CANDIDATES}
        selectedIds={['a']}
        onChange={vi.fn()}
        getId={(c) => c.id}
        getLabel={(c) => c.label}
        addLabel="Add entry"
        emptyLabel="Nothing selected yet."
      />,
    )

    expect(screen.getByText('Alpha')).toBeInTheDocument()
    expect(screen.getByRole('option', { name: 'Beta' })).toBeInTheDocument()
    expect(screen.queryByRole('option', { name: 'Alpha' })).not.toBeInTheDocument()
  })

  it('appends the chosen candidate to the end of the selection', async () => {
    const onChange = vi.fn()
    render(
      <SelectionOrderEditor
        candidates={CANDIDATES}
        selectedIds={['a']}
        onChange={onChange}
        getId={(c) => c.id}
        getLabel={(c) => c.label}
        addLabel="Add entry"
        emptyLabel="Nothing selected yet."
      />,
    )

    await userEvent.selectOptions(screen.getByLabelText('Add entry'), 'b')

    expect(onChange).toHaveBeenCalledWith(['a', 'b'])
  })

  it('moves a selected entry down then up', async () => {
    const onChange = vi.fn()
    render(
      <SelectionOrderEditor
        candidates={CANDIDATES}
        selectedIds={['a', 'b']}
        onChange={onChange}
        getId={(c) => c.id}
        getLabel={(c) => c.label}
        addLabel="Add entry"
        emptyLabel="Nothing selected yet."
      />,
    )

    await userEvent.click(screen.getByRole('button', { name: 'Move Alpha down' }))

    expect(onChange).toHaveBeenCalledWith(['b', 'a'])
  })

  it('disables move-up for the first row and move-down for the last row', () => {
    render(
      <SelectionOrderEditor
        candidates={CANDIDATES}
        selectedIds={['a', 'b']}
        onChange={vi.fn()}
        getId={(c) => c.id}
        getLabel={(c) => c.label}
        addLabel="Add entry"
        emptyLabel="Nothing selected yet."
      />,
    )

    expect(screen.getByRole('button', { name: 'Move Alpha up' })).toBeDisabled()
    expect(screen.getByRole('button', { name: 'Move Beta down' })).toBeDisabled()
  })

  it('removes a selected entry', async () => {
    const onChange = vi.fn()
    render(
      <SelectionOrderEditor
        candidates={CANDIDATES}
        selectedIds={['a', 'b']}
        onChange={onChange}
        getId={(c) => c.id}
        getLabel={(c) => c.label}
        addLabel="Add entry"
        emptyLabel="Nothing selected yet."
      />,
    )

    await userEvent.click(screen.getAllByRole('button', { name: 'Remove' })[0])

    expect(onChange).toHaveBeenCalledWith(['b'])
  })

  it('skips a selected id that no longer resolves to a candidate', () => {
    render(
      <SelectionOrderEditor
        candidates={CANDIDATES}
        selectedIds={['a', 'missing']}
        onChange={vi.fn()}
        getId={(c) => c.id}
        getLabel={(c) => c.label}
        addLabel="Add entry"
        emptyLabel="Nothing selected yet."
      />,
    )

    expect(screen.getByText('Alpha')).toBeInTheDocument()
    expect(screen.queryByText('missing')).not.toBeInTheDocument()
  })
})
