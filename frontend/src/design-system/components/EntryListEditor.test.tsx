import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { EntryListEditor } from './EntryListEditor'

type Row = { id: string; name: string }

function renderRow(entry: Row, onChange: (next: Row) => void, onRemove: () => void) {
  return (
    <>
      <input aria-label={`Name for ${entry.id}`} value={entry.name} onChange={(event) => onChange({ ...entry, name: event.target.value })} />
      <button type="button" onClick={onRemove}>
        Remove {entry.name || entry.id}
      </button>
    </>
  )
}

describe('EntryListEditor', () => {
  it('shows the empty label when there are no entries', () => {
    render(
      <EntryListEditor<Row>
        entries={[]}
        onChange={vi.fn()}
        createEntry={() => ({ id: '1', name: '' })}
        getKey={(entry) => entry.id}
        addLabel="Add row"
        emptyLabel="No rows yet."
        renderEntry={renderRow}
      />,
    )

    expect(screen.getByText('No rows yet.')).toBeInTheDocument()
  })

  it('appends a fresh entry when Add is clicked', async () => {
    const onChange = vi.fn()
    render(
      <EntryListEditor<Row>
        entries={[{ id: '1', name: 'Existing' }]}
        onChange={onChange}
        createEntry={() => ({ id: '2', name: '' })}
        getKey={(entry) => entry.id}
        addLabel="Add row"
        emptyLabel="No rows yet."
        renderEntry={renderRow}
      />,
    )

    await userEvent.click(screen.getByRole('button', { name: 'Add row' }))

    expect(onChange).toHaveBeenCalledWith([
      { id: '1', name: 'Existing' },
      { id: '2', name: '' },
    ])
  })

  it('updates only the matching entry in place', async () => {
    const onChange = vi.fn()
    render(
      <EntryListEditor<Row>
        entries={[
          { id: '1', name: 'First' },
          { id: '2', name: 'Second' },
        ]}
        onChange={onChange}
        createEntry={() => ({ id: '3', name: '' })}
        getKey={(entry) => entry.id}
        addLabel="Add row"
        emptyLabel="No rows yet."
        renderEntry={renderRow}
      />,
    )

    await userEvent.type(screen.getByLabelText('Name for 2'), '!')

    expect(onChange).toHaveBeenCalledWith([
      { id: '1', name: 'First' },
      { id: '2', name: 'Second!' },
    ])
  })

  it('removes only the targeted entry', async () => {
    const onChange = vi.fn()
    render(
      <EntryListEditor<Row>
        entries={[
          { id: '1', name: 'First' },
          { id: '2', name: 'Second' },
        ]}
        onChange={onChange}
        createEntry={() => ({ id: '3', name: '' })}
        getKey={(entry) => entry.id}
        addLabel="Add row"
        emptyLabel="No rows yet."
        renderEntry={renderRow}
      />,
    )

    await userEvent.click(screen.getByRole('button', { name: 'Remove First' }))

    expect(onChange).toHaveBeenCalledWith([{ id: '2', name: 'Second' }])
  })
})
