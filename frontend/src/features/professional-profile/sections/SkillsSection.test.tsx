import { useState } from 'react'
import { describe, it, expect } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { server } from '../../../mocks/server'
import type { SkillDto } from '../api'
import { SkillsSection } from './SkillsSection'

const SKILL: SkillDto = { id: 's1', displayName: 'TypeScript', normalizedKey: 'typescript', category: 'Language', proficiency: 'Advanced' }

// SkillsSection is controlled by its parent (ProfessionalProfilePage in production) — this stands
// in for that parent so the section's own onChange/save round trip behaves the same as it would
// there, without pulling in the whole page.
function Harness() {
  const [skills, setSkills] = useState<SkillDto[]>([SKILL])
  return <SkillsSection skills={skills} onChange={setSkills} />
}

describe('SkillsSection save reliability', () => {
  it('Done succeeds and closes edit mode', async () => {
    server.use(http.put('/api/professional-profile/skills', () => new HttpResponse(null, { status: 204 })))

    render(<Harness />)
    await userEvent.click(screen.getByRole('button', { name: 'Edit TypeScript' }))
    expect(screen.getByLabelText('Skill')).toBeInTheDocument()

    await userEvent.click(screen.getByRole('button', { name: 'Done editing TypeScript' }))

    await screen.findByRole('button', { name: 'Edit TypeScript' })
    expect(screen.queryByLabelText('Skill')).not.toBeInTheDocument()
    expect(screen.queryByRole('alert')).not.toBeInTheDocument()
  })

  it('Done fails and the row stays editable with an error shown', async () => {
    server.use(http.put('/api/professional-profile/skills', () => new HttpResponse(null, { status: 500 })))

    render(<Harness />)
    await userEvent.click(screen.getByRole('button', { name: 'Edit TypeScript' }))
    await userEvent.click(screen.getByRole('button', { name: 'Done editing TypeScript' }))

    expect(await screen.findByRole('alert')).toBeInTheDocument()
    // Still editable — the failed save must not have closed the row as if it had persisted.
    expect(screen.getByLabelText('Skill')).toBeInTheDocument()
  })

  it('a failed delete does not leave the skill removed', async () => {
    server.use(http.put('/api/professional-profile/skills', () => new HttpResponse(null, { status: 500 })))

    render(<Harness />)
    await userEvent.click(screen.getByRole('button', { name: 'Remove TypeScript' }))

    expect(await screen.findByRole('alert')).toBeInTheDocument()
    // The optimistic removal must be reverted, not presented as saved.
    expect(screen.getByText('TypeScript')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Remove TypeScript' })).toBeInTheDocument()
  })
})
