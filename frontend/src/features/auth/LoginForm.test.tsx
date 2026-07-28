import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { LoginForm } from './LoginForm'

const { postMock } = vi.hoisted(() => ({ postMock: vi.fn() }))

vi.mock('../../api/client', () => ({
  apiClient: { POST: postMock },
}))

describe('LoginForm', () => {
  it('shows the generic confirmation message after submitting an email', async () => {
    postMock.mockResolvedValue({
      data: { message: 'If that email is registered, a sign-in link has been sent.' },
    })

    render(<LoginForm />)
    await userEvent.type(screen.getByLabelText('Email'), 'owner@example.com')
    await userEvent.click(screen.getByRole('button', { name: 'Send sign-in link' }))

    expect(await screen.findByText('If that email is registered, a sign-in link has been sent.')).toBeInTheDocument()
    expect(postMock).toHaveBeenCalledWith('/auth/login', { body: { email: 'owner@example.com' } })
  })
})
