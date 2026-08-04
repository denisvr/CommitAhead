import { describe, it, expect } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { server } from '../../mocks/server'
import { LoginForm } from './LoginForm'

describe('LoginForm', () => {
  it('shows the generic confirmation message after submitting an email', async () => {
    let capturedBody: unknown
    server.use(
      http.post('/auth/login', async ({ request }) => {
        capturedBody = await request.json()
        return HttpResponse.json({ message: 'If that email is registered, a sign-in link has been sent.' })
      }),
    )

    render(<LoginForm />)
    await userEvent.type(screen.getByLabelText('Email'), 'owner@example.com')
    await userEvent.click(screen.getByRole('button', { name: 'Send sign-in link' }))

    expect(await screen.findByText('If that email is registered, a sign-in link has been sent.')).toBeInTheDocument()
    expect(capturedBody).toEqual({ email: 'owner@example.com' })
  })

  it('shows the same generic message even when the backend call fails (never reveals whether the email is registered)', async () => {
    server.use(http.post('/auth/login', () => new HttpResponse(null, { status: 500 })))

    render(<LoginForm />)
    await userEvent.type(screen.getByLabelText('Email'), 'owner@example.com')
    await userEvent.click(screen.getByRole('button', { name: 'Send sign-in link' }))

    expect(await screen.findByText('If that email is registered, a sign-in link has been sent.')).toBeInTheDocument()
  })
})
