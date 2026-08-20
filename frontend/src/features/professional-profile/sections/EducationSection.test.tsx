import { useState } from 'react'
import { describe, it, expect } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { server } from '../../../mocks/server'
import type { EducationEntryDto } from '../api'
import { EducationSection } from './EducationSection'

const ENTRIES: EducationEntryDto[] = [
  { id: 'e1', institution: 'Alpha University', degree: 'BSc', field: null, startDate: null, endDate: null, location: null, detailsMarkdown: null },
  { id: 'e2', institution: 'Beta College', degree: 'MSc', field: null, startDate: null, endDate: null, location: null, detailsMarkdown: null },
]

// EducationSection is controlled by its parent (ProfessionalProfilePage in production) — this
// stands in for that parent so the section's own onChange/save round trip behaves the same as it
// would there.
function Harness() {
  const [education, setEducation] = useState<EducationEntryDto[]>(ENTRIES)
  return <EducationSection education={education} onChange={setEducation} />
}

const degreeOrder = () => screen.getAllByText(/^(BSc|MSc)$/).map((el) => el.textContent)

// Move up/down only render once a row is expanded — open the BSc row first, the same way a user
// would before ever seeing the button.
const openBscRow = async () => {
  await userEvent.click(screen.getByText('BSc').closest('button')!)
  return screen.getByRole('button', { name: 'Move BSc down' })
}

describe('EducationSection reorder reliability', () => {
  it('a failed reorder reverts to the previously persisted order', async () => {
    server.use(http.put('/api/professional-profile/education', () => new HttpResponse(null, { status: 500 })))

    render(<Harness />)
    expect(degreeOrder()).toEqual(['BSc', 'MSc'])
    const moveDown = await openBscRow()

    await userEvent.click(moveDown)

    expect(await screen.findByRole('alert')).toBeInTheDocument()
    // The optimistic reorder must be reverted once the PUT fails, not left looking saved.
    await waitFor(() => expect(degreeOrder()).toEqual(['BSc', 'MSc']))
  })

  it('a successful reorder persists the new order', async () => {
    server.use(http.put('/api/professional-profile/education', () => new HttpResponse(null, { status: 204 })))

    render(<Harness />)
    const moveDown = await openBscRow()

    await userEvent.click(moveDown)

    await waitFor(() => expect(degreeOrder()).toEqual(['MSc', 'BSc']))
    expect(screen.queryByRole('alert')).not.toBeInTheDocument()
  })
})
