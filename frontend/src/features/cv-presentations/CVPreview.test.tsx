import { describe, it, expect } from 'vitest'
import { render, screen } from '@testing-library/react'
import { CVPreview } from './CVPreview'
import type { ProfessionalProfileResponse } from '../professional-profile/api'
import type { CVPresentationResponse } from './api'

const PROFILE: ProfessionalProfileResponse = {
  id: 'profile-1',
  contactInfo: { name: 'Ada Lovelace', email: 'ada@example.com', phone: '+44 20 7946 0958', address: '10 Downing Street', photoStorageKey: null },
  summaryMarkdown: 'Profile summary.',
  experience: [
    {
      id: 'exp-1',
      company: 'Acme',
      client: null,
      role: 'Engineer',
      employmentType: 'Permanent',
      startDate: { year: 2018, month: 1 },
      endDate: { year: 2020, month: 6 },
      location: null,
      workMode: 'Remote',
      summaryMarkdown: 'First role.',
      achievements: [],
      skillIds: ['skill-1'],
    },
    {
      id: 'exp-2',
      company: 'Globex',
      client: null,
      role: 'Senior Engineer',
      employmentType: 'Permanent',
      startDate: { year: 2020, month: 7 },
      endDate: null,
      location: null,
      workMode: 'Remote',
      summaryMarkdown: 'Second role.',
      achievements: [],
      skillIds: [],
    },
  ],
  education: [],
  skills: [{ id: 'skill-1', displayName: 'C#', normalizedKey: 'c#', category: 'Language', proficiency: null }],
  languages: [],
  certifications: [],
  projects: [],
  profileLinks: [{ id: 'link-1', kind: 'GitHub', label: 'My GitHub', url: 'https://github.com/ada' }],
  createdAtUtc: '2024-01-01T00:00:00Z',
  updatedAtUtc: '2024-01-01T00:00:00Z',
}

const BASE_PRESENTATION: CVPresentationResponse = {
  id: 'presentation-1',
  professionalProfileId: 'profile-1',
  label: 'UK — Senior Backend Engineer',
  targetMarket: 'United Kingdom',
  targetRole: 'Senior Backend Engineer',
  locale: 'en-GB',
  templateKey: 'modern-one-page',
  summaryOverrideMarkdown: null,
  includePhoto: false,
  includeEmail: true,
  includePhone: true,
  includeAddress: true,
  dateFormat: 'dd MMM yyyy',
  pageLimit: 2,
  experienceSelections: [],
  educationSelections: [],
  skillSelections: [],
  languageSelections: [],
  certificationSelections: [],
  projectSelections: [],
  profileLinkSelections: [],
  createdAtUtc: '2024-01-01T00:00:00Z',
  updatedAtUtc: '2024-01-01T00:00:00Z',
}

describe('CVPreview visibility rules', () => {
  it('shows contact fields only when their include flag is set', () => {
    render(<CVPreview profile={PROFILE} presentation={{ ...BASE_PRESENTATION, includeEmail: false, includePhone: true, includeAddress: false }} />)

    expect(screen.queryByText('ada@example.com')).not.toBeInTheDocument()
    expect(screen.getByText('+44 20 7946 0958')).toBeInTheDocument()
    expect(screen.queryByText('10 Downing Street')).not.toBeInTheDocument()
  })

  it('shows every contact field when every flag is set', () => {
    render(<CVPreview profile={PROFILE} presentation={{ ...BASE_PRESENTATION, includeEmail: true, includePhone: true, includeAddress: true }} />)

    expect(screen.getByText('ada@example.com')).toBeInTheDocument()
    expect(screen.getByText('+44 20 7946 0958')).toBeInTheDocument()
    expect(screen.getByText('10 Downing Street')).toBeInTheDocument()
  })

  it('renders the summary override instead of the profile summary when set', () => {
    render(<CVPreview profile={PROFILE} presentation={{ ...BASE_PRESENTATION, summaryOverrideMarkdown: 'Override summary.' }} />)

    expect(screen.getByText('Override summary.')).toBeInTheDocument()
    expect(screen.queryByText('Profile summary.')).not.toBeInTheDocument()
  })

  it('falls back to the profile summary when there is no override', () => {
    render(<CVPreview profile={PROFILE} presentation={BASE_PRESENTATION} />)

    expect(screen.getByText('Profile summary.')).toBeInTheDocument()
  })
})

describe('CVPreview selection ordering and resolution', () => {
  it('renders selected experience entries in selection order, not profile array order', () => {
    render(<CVPreview profile={PROFILE} presentation={{ ...BASE_PRESENTATION, experienceSelections: ['exp-2', 'exp-1'] }} />)

    const headings = screen.getAllByText(/Engineer —/).map((node) => node.textContent)
    expect(headings).toEqual(['Senior Engineer — Globex', 'Engineer — Acme'])
  })

  it('resolves skill names for a selected experience entry via skillIds', () => {
    render(<CVPreview profile={PROFILE} presentation={{ ...BASE_PRESENTATION, experienceSelections: ['exp-1'] }} />)

    expect(screen.getByText('C#')).toBeInTheDocument()
  })

  it('skips a selected id that no longer resolves to a canonical entry, instead of crashing', () => {
    render(<CVPreview profile={PROFILE} presentation={{ ...BASE_PRESENTATION, experienceSelections: ['exp-1', 'dangling-id'] }} />)

    expect(screen.getByText('Engineer — Acme')).toBeInTheDocument()
  })

  it('renders profile links as safe, clickable links', () => {
    render(<CVPreview profile={PROFILE} presentation={{ ...BASE_PRESENTATION, profileLinkSelections: ['link-1'] }} />)

    expect(screen.getByRole('link', { name: 'My GitHub' })).toHaveAttribute('href', 'https://github.com/ada')
  })

  it('does not render an Experience section at all when nothing is selected', () => {
    render(<CVPreview profile={PROFILE} presentation={BASE_PRESENTATION} />)

    expect(screen.queryByRole('heading', { name: 'Experience' })).not.toBeInTheDocument()
  })
})
