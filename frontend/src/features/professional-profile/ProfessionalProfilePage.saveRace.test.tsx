import { describe, it, expect } from 'vitest'
import { render, screen, waitFor, within } from '@testing-library/react'
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
  skills: [{ id: 'skill-1', displayName: 'TypeScript', normalizedKey: 'typescript', category: 'Language', proficiency: null }],
  languages: [{ id: 'lang-1', language: 'English', proficiency: 'C1', certification: null }],
  certifications: [],
  projects: [],
  profileLinks: [],
  createdAtUtc: '2024-01-01T00:00:00Z',
  updatedAtUtc: '2024-01-01T00:00:00Z',
}

describe('ProfessionalProfilePage cross-section save race', () => {
  it('a failed save/rollback in one section does not overwrite a newer successful change in another', async () => {
    server.use(http.get('/api/professional-profile', () => HttpResponse.json(PROFILE)))

    // Held open until the test explicitly resolves it, so the Skills PUT is still in flight while
    // the Languages edit below completes — the exact overlap the bug needed to reproduce.
    let resolveSkillsPut: () => void = () => {}
    server.use(
      http.put(
        '/api/professional-profile/skills',
        () =>
          new Promise<Response>((resolve) => {
            resolveSkillsPut = () => resolve(new HttpResponse(null, { status: 500 }))
          }),
      ),
      http.put('/api/professional-profile/languages', () => new HttpResponse(null, { status: 204 })),
    )

    render(<ProfessionalProfilePage />)
    await screen.findByRole('heading', { name: 'About you' })

    // Start a Skills delete — the optimistic removal applies immediately; its PUT stays pending.
    await userEvent.click(screen.getByRole('button', { name: 'Remove TypeScript' }))
    expect(screen.queryByText('TypeScript')).not.toBeInTheDocument()

    // While that save is still in flight, make and successfully save a genuinely newer change to
    // a *different* section of the same profile.
    await userEvent.click(screen.getByRole('button', { name: 'Edit English' }))
    await userEvent.clear(screen.getByLabelText('Certification'))
    await userEvent.type(screen.getByLabelText('Certification'), 'C1 certified')
    await userEvent.click(screen.getByRole('button', { name: 'Done editing English' }))
    await waitFor(() => expect(screen.getByText('C1 certified')).toBeInTheDocument())

    // Now let the pending Skills PUT fail and roll back.
    resolveSkillsPut()
    const skillsCard = document.getElementById('skills') as HTMLElement
    await within(skillsCard).findByText('TypeScript')

    // The regression this guards against: a rollback built from a `{ ...profile, skills }`
    // snapshot captured before the Languages edit would silently discard that edit here, since
    // that stale snapshot's `languages` field still has the pre-edit value.
    expect(screen.getByText('C1 certified')).toBeInTheDocument()
  })
})
