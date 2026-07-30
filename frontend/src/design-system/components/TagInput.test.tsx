import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { TagInput } from './TagInput'

describe('TagInput', () => {
  it('adds a tag when Enter is pressed', async () => {
    const onChange = vi.fn()
    render(<TagInput label="Tags" value={[]} onChange={onChange} />)

    await userEvent.type(screen.getByLabelText('Tags'), 'Arrays{Enter}')

    expect(onChange).toHaveBeenCalledWith(['Arrays'])
  })

  it('adds the draft tag on blur', async () => {
    const onChange = vi.fn()
    render(<TagInput label="Tags" value={[]} onChange={onChange} />)

    await userEvent.type(screen.getByLabelText('Tags'), 'Hash Table')
    await userEvent.tab()

    expect(onChange).toHaveBeenCalledWith(['Hash Table'])
  })

  it('renders existing tags as removable chips', () => {
    render(<TagInput label="Tags" value={['arrays', 'two-pointers']} onChange={vi.fn()} />)

    expect(screen.getByText('arrays')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Remove tag arrays' })).toBeInTheDocument()
  })

  it('removes a tag when its remove button is clicked', async () => {
    const onChange = vi.fn()
    render(<TagInput label="Tags" value={['arrays', 'two-pointers']} onChange={onChange} />)

    await userEvent.click(screen.getByRole('button', { name: 'Remove tag arrays' }))

    expect(onChange).toHaveBeenCalledWith(['two-pointers'])
  })

  it('removes the last tag on backspace when the input is empty', async () => {
    const onChange = vi.fn()
    render(<TagInput label="Tags" value={['arrays', 'two-pointers']} onChange={onChange} />)

    screen.getByLabelText('Tags').focus()
    await userEvent.keyboard('{Backspace}')

    expect(onChange).toHaveBeenCalledWith(['arrays'])
  })
})
