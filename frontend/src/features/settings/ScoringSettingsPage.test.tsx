import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { ScoringSettingsPage } from './ScoringSettingsPage'

const { fetchScoringConfigMock, updateScoringConfigMock, resetScoringConfigMock } = vi.hoisted(() => ({
  fetchScoringConfigMock: vi.fn(),
  updateScoringConfigMock: vi.fn(),
  resetScoringConfigMock: vi.fn(),
}))

vi.mock('../study-items/api', async () => {
  const actual = await vi.importActual<typeof import('../study-items/api')>('../study-items/api')
  return {
    ...actual,
    fetchScoringConfig: fetchScoringConfigMock,
    updateScoringConfig: updateScoringConfigMock,
    resetScoringConfig: resetScoringConfigMock,
  }
})

const DEFAULTS = { importanceWeight: 40, demandWeight: 35, masteryGapWeight: 25, isOverridden: false }
const OVERRIDE = { importanceWeight: 50, demandWeight: 30, masteryGapWeight: 20, isOverridden: true }

describe('ScoringSettingsPage', () => {
  beforeEach(() => {
    fetchScoringConfigMock.mockReset()
    updateScoringConfigMock.mockReset()
    resetScoringConfigMock.mockReset()
  })

  it('shows a loading state before the config resolves', () => {
    fetchScoringConfigMock.mockReturnValue(new Promise(() => {}))

    render(<ScoringSettingsPage />)

    expect(screen.getByRole('status')).toHaveTextContent(/loading/i)
  })

  it('loads and displays the effective weights, indicating defaults are in use', async () => {
    fetchScoringConfigMock.mockResolvedValue(DEFAULTS)

    render(<ScoringSettingsPage />)

    expect(await screen.findByText(/using default weights/i)).toBeInTheDocument()
    expect(screen.getByLabelText('Importance weight')).toHaveValue(40)
    expect(screen.getByLabelText('Demand weight')).toHaveValue(35)
    expect(screen.getByLabelText('Mastery-gap weight')).toHaveValue(25)
  })

  it('indicates when custom weights are in effect', async () => {
    fetchScoringConfigMock.mockResolvedValue(OVERRIDE)

    render(<ScoringSettingsPage />)

    expect(await screen.findByText(/using custom weights/i)).toBeInTheDocument()
  })

  it('rejects weights that do not sum to 100 without calling the API', async () => {
    fetchScoringConfigMock.mockResolvedValue(DEFAULTS)

    render(<ScoringSettingsPage />)
    await screen.findByLabelText('Importance weight')

    await userEvent.clear(screen.getByLabelText('Importance weight'))
    await userEvent.type(screen.getByLabelText('Importance weight'), '50')
    await userEvent.click(screen.getByRole('button', { name: 'Save' }))

    expect(await screen.findByRole('alert')).toHaveTextContent(/must sum to exactly 100/i)
    expect(updateScoringConfigMock).not.toHaveBeenCalled()
  })

  it('saves valid weights and reflects the refreshed config', async () => {
    fetchScoringConfigMock.mockResolvedValueOnce(DEFAULTS).mockResolvedValueOnce(OVERRIDE)
    updateScoringConfigMock.mockResolvedValue(undefined)

    render(<ScoringSettingsPage />)
    await screen.findByLabelText('Importance weight')

    await userEvent.click(screen.getByRole('button', { name: 'Save' }))

    expect(updateScoringConfigMock).toHaveBeenCalledWith(40, 35, 25)
    expect(await screen.findByText(/using custom weights/i)).toBeInTheDocument()
  })

  it('shows a server-side validation error from the API without crashing', async () => {
    fetchScoringConfigMock.mockResolvedValue(DEFAULTS)
    updateScoringConfigMock.mockRejectedValue(new Error('Weights must be non-negative and sum to 100.'))

    render(<ScoringSettingsPage />)
    await screen.findByLabelText('Importance weight')

    await userEvent.click(screen.getByRole('button', { name: 'Save' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('Weights must be non-negative and sum to 100.')
  })

  it('resets to the default weights', async () => {
    fetchScoringConfigMock.mockResolvedValueOnce(OVERRIDE).mockResolvedValueOnce(DEFAULTS)
    resetScoringConfigMock.mockResolvedValue(undefined)

    render(<ScoringSettingsPage />)
    await screen.findByText(/using custom weights/i)

    await userEvent.click(screen.getByRole('button', { name: 'Reset to defaults' }))

    expect(resetScoringConfigMock).toHaveBeenCalled()
    expect(await screen.findByText(/using default weights/i)).toBeInTheDocument()
    expect(screen.getByLabelText('Importance weight')).toHaveValue(40)
  })

  it('shows a retryable error when the initial load fails', async () => {
    fetchScoringConfigMock.mockRejectedValueOnce(new Error('Network down')).mockResolvedValueOnce(DEFAULTS)

    render(<ScoringSettingsPage />)

    expect(await screen.findByRole('alert')).toHaveTextContent('Network down')
    await userEvent.click(screen.getByRole('button', { name: 'Try again' }))

    expect(await screen.findByLabelText('Importance weight')).toBeInTheDocument()
  })
})
