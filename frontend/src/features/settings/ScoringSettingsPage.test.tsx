import { describe, it, expect } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { delay, http, HttpResponse } from 'msw'
import { server } from '../../mocks/server'
import { ScoringSettingsPage } from './ScoringSettingsPage'

const DEFAULTS = { importanceWeight: 40, demandWeight: 35, masteryGapWeight: 25, isOverridden: false }
const OVERRIDE = { importanceWeight: 50, demandWeight: 30, masteryGapWeight: 20, isOverridden: true }

describe('ScoringSettingsPage', () => {
  it('shows a loading state before the config resolves', async () => {
    server.use(
      http.get('/api/scoring-config', async () => {
        await delay('infinite')
      }),
    )

    render(<ScoringSettingsPage />)

    expect(screen.getByRole('status')).toHaveTextContent(/loading/i)
  })

  it('loads and displays the effective weights, indicating defaults are in use', async () => {
    server.use(http.get('/api/scoring-config', () => HttpResponse.json(DEFAULTS)))

    render(<ScoringSettingsPage />)

    expect(await screen.findByText(/using default weights/i)).toBeInTheDocument()
    expect(screen.getByLabelText('Importance weight')).toHaveValue(40)
    expect(screen.getByLabelText('Demand weight')).toHaveValue(35)
    expect(screen.getByLabelText('Mastery-gap weight')).toHaveValue(25)
  })

  it('indicates when custom weights are in effect', async () => {
    server.use(http.get('/api/scoring-config', () => HttpResponse.json(OVERRIDE)))

    render(<ScoringSettingsPage />)

    expect(await screen.findByText(/using custom weights/i)).toBeInTheDocument()
  })

  it('rejects weights that do not sum to 100 without calling the API', async () => {
    server.use(http.get('/api/scoring-config', () => HttpResponse.json(DEFAULTS)))
    let putCalled = false
    server.use(http.put('/api/scoring-config', () => {
      putCalled = true
      return new HttpResponse(null, { status: 204 })
    }))

    render(<ScoringSettingsPage />)
    await screen.findByLabelText('Importance weight')

    await userEvent.clear(screen.getByLabelText('Importance weight'))
    await userEvent.type(screen.getByLabelText('Importance weight'), '50')
    await userEvent.click(screen.getByRole('button', { name: 'Save' }))

    expect(await screen.findByRole('alert')).toHaveTextContent(/must sum to exactly 100/i)
    expect(putCalled).toBe(false)
  })

  it('saves valid weights and reflects the refreshed config', async () => {
    let current = DEFAULTS
    server.use(
      http.get('/api/scoring-config', () => HttpResponse.json(current)),
      http.put('/api/scoring-config', () => {
        current = OVERRIDE
        return new HttpResponse(null, { status: 204 })
      }),
    )

    render(<ScoringSettingsPage />)
    await screen.findByLabelText('Importance weight')

    await userEvent.click(screen.getByRole('button', { name: 'Save' }))

    expect(await screen.findByText(/using custom weights/i)).toBeInTheDocument()
  })

  it('shows a server-side validation error from the API without crashing', async () => {
    server.use(
      http.get('/api/scoring-config', () => HttpResponse.json(DEFAULTS)),
      http.put('/api/scoring-config', () =>
        HttpResponse.json({ title: 'Validation failed.', detail: 'Weights must be non-negative and sum to 100.' }, { status: 422 }),
      ),
    )

    render(<ScoringSettingsPage />)
    await screen.findByLabelText('Importance weight')

    await userEvent.click(screen.getByRole('button', { name: 'Save' }))

    expect(await screen.findByRole('alert')).toHaveTextContent(/status 422/i)
  })

  it('resets to the default weights', async () => {
    let current: typeof DEFAULTS = OVERRIDE
    server.use(
      http.get('/api/scoring-config', () => HttpResponse.json(current)),
      http.delete('/api/scoring-config', () => {
        current = DEFAULTS
        return new HttpResponse(null, { status: 204 })
      }),
    )

    render(<ScoringSettingsPage />)
    await screen.findByText(/using custom weights/i)

    await userEvent.click(screen.getByRole('button', { name: 'Reset to defaults' }))

    expect(await screen.findByText(/using default weights/i)).toBeInTheDocument()
    expect(screen.getByLabelText('Importance weight')).toHaveValue(40)
  })

  it('shows a retryable error when the initial load fails', async () => {
    let callCount = 0
    server.use(
      http.get('/api/scoring-config', () => {
        callCount += 1
        return callCount === 1 ? HttpResponse.error() : HttpResponse.json(DEFAULTS)
      }),
    )

    render(<ScoringSettingsPage />)

    expect(await screen.findByRole('alert')).toBeInTheDocument()
    await userEvent.click(screen.getByRole('button', { name: 'Try again' }))

    expect(await screen.findByLabelText('Importance weight')).toBeInTheDocument()
  })
})
