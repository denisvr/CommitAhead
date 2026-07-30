import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { RatingScale } from './RatingScale'

describe('RatingScale', () => {
  it('renders a radiogroup with the selected value checked', () => {
    render(<RatingScale label="Importance" value={3} onChange={vi.fn()} />)

    const group = screen.getByRole('radiogroup', { name: 'Importance' })
    expect(group).toBeInTheDocument()
    expect(screen.getByRole('radio', { name: '3' })).toHaveAttribute('aria-checked', 'true')
    expect(screen.getByRole('radio', { name: '1' })).toHaveAttribute('aria-checked', 'false')
  })

  it('calls onChange and moves focus when a rating is clicked', async () => {
    const onChange = vi.fn()
    render(<RatingScale label="Importance" value={3} onChange={onChange} />)

    await userEvent.click(screen.getByRole('radio', { name: '5' }))

    expect(onChange).toHaveBeenCalledWith(5)
    expect(screen.getByRole('radio', { name: '5' })).toHaveFocus()
  })

  it('supports arrow-key navigation between options', async () => {
    const onChange = vi.fn()
    render(<RatingScale label="Importance" value={3} onChange={onChange} />)

    screen.getByRole('radio', { name: '3' }).focus()
    await userEvent.keyboard('{ArrowRight}')

    expect(onChange).toHaveBeenCalledWith(4)
  })

  it('clamps at the boundaries', async () => {
    const onChange = vi.fn()
    render(<RatingScale label="Importance" value={5} onChange={onChange} />)

    screen.getByRole('radio', { name: '5' }).focus()
    await userEvent.keyboard('{ArrowRight}')

    expect(onChange).toHaveBeenCalledWith(5)
  })

  it('only the checked option is tab-reachable (roving tabindex)', () => {
    render(<RatingScale label="Importance" value={2} onChange={vi.fn()} />)

    expect(screen.getByRole('radio', { name: '2' })).toHaveAttribute('tabIndex', '0')
    expect(screen.getByRole('radio', { name: '1' })).toHaveAttribute('tabIndex', '-1')
  })
})
