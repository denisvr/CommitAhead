import { beforeAll, describe, expect, it } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { server } from '../../mocks/server'
import { ProfessionalProfilePage } from './ProfessionalProfilePage'

const PROFILE = {
  id: 'profile-1',
  contactInfo: { name: 'Ada Lovelace', email: 'ada@example.com', phone: null, address: null, photoStorageKey: null },
  summaryMarkdown: 'Backend engineer.',
  experience: [],
  education: [],
  skills: [],
  languages: [],
  certifications: [],
  projects: [],
  profileLinks: [],
  createdAtUtc: '2024-01-01T00:00:00Z',
  updatedAtUtc: '2024-01-01T00:00:00Z',
}

// jsdom has no real <dialog> implementation (no showModal/close) as of this project's jsdom
// version, so this polyfills just enough of the native behaviour our code relies on to exercise
// the same open/close wiring a real browser would. The actual regression this dialog once had —
// `display: flex` on the bare `.previewDialog` selector overriding Chromium's own
// `dialog:not([open]) { display: none }`, so a *closed* dialog stayed visible — is a real-browser
// rendering concern CSS Modules aren't even loaded for in this test environment; that is covered
// by the Playwright extension in e2e/tests/journeys/004-cv-presentation-export.spec.ts instead.
// This suite only proves the component calls showModal()/close() at the right times.
beforeAll(() => {
  if (!HTMLDialogElement.prototype.showModal) {
    HTMLDialogElement.prototype.showModal = function (this: HTMLDialogElement) {
      this.setAttribute('open', '')
    }
  }
  if (!HTMLDialogElement.prototype.close) {
    HTMLDialogElement.prototype.close = function (this: HTMLDialogElement) {
      this.removeAttribute('open')
    }
  }
})

describe('ProfessionalProfilePage preview dialog', () => {
  it('starts closed; the Preview control opens it; the close button and a backdrop click close it again', async () => {
    server.use(http.get('/api/professional-profile', () => HttpResponse.json(PROFILE)))

    render(<ProfessionalProfilePage />)
    await screen.findByRole('heading', { name: 'About you' })

    const dialog = document.querySelector('dialog')
    if (!dialog) throw new Error('Expected a <dialog> element to be rendered.')

    expect(dialog.hasAttribute('open')).toBe(false)

    await userEvent.click(screen.getByRole('button', { name: 'Preview' }))
    expect(dialog.hasAttribute('open')).toBe(true)

    await userEvent.click(screen.getByRole('button', { name: 'Close preview' }))
    expect(dialog.hasAttribute('open')).toBe(false)

    // Re-open, then close via a click whose target is the <dialog> element itself — the backdrop
    // area — not its content. The component's onClick only closes when event.target === the
    // dialog, so this also proves a click inside the dialog's own content would not close it.
    await userEvent.click(screen.getByRole('button', { name: 'Preview' }))
    expect(dialog.hasAttribute('open')).toBe(true)

    dialog.dispatchEvent(new MouseEvent('click', { bubbles: true }))
    expect(dialog.hasAttribute('open')).toBe(false)
  })
})
