import { apiClient } from '../../api/client'
import type { components } from '../../api/generated/schema'

export type ContactInfoDto = components['schemas']['ContactInfoDto']
export type YearMonthDto = components['schemas']['YearMonthDto']
export type ExperienceEntryDto = components['schemas']['ExperienceEntryDto']
export type EducationEntryDto = components['schemas']['EducationEntryDto']
export type SkillDto = components['schemas']['SkillDto']
export type LanguageEntryDto = components['schemas']['LanguageEntryDto']
export type CertificationEntryDto = components['schemas']['CertificationEntryDto']
export type ProjectEntryDto = components['schemas']['ProjectEntryDto']
export type ProfileLinkDto = components['schemas']['ProfileLinkDto']
export type EmploymentType = components['schemas']['EmploymentType']
export type WorkMode = components['schemas']['WorkMode']
export type SkillCategory = components['schemas']['SkillCategory']
export type SkillProficiency = components['schemas']['SkillProficiency']
export type LanguageProficiency = components['schemas']['LanguageProficiency']
export type ProfileLinkKind = components['schemas']['ProfileLinkKind']
export type ProfessionalProfileResponse = components['schemas']['ProfessionalProfileResponse']

// See study-items/api.ts for why this widening-narrow exists — every response here always sends a
// real JSON number, this just narrows openapi-typescript's widened `number | string` back.
export function toNumber(value: number | string): number {
  return typeof value === 'number' ? value : Number(value)
}

async function csrfHeaders(): Promise<{ 'X-CSRF-TOKEN': string }> {
  const { data } = await apiClient.GET('/auth/csrf')
  if (!data) {
    throw new Error('Could not obtain a CSRF token.')
  }

  return { 'X-CSRF-TOKEN': data.token }
}

async function describeError(response: Response, fallback: string): Promise<string> {
  try {
    const body = (await response.clone().json()) as { message?: string }
    if (body?.message) {
      return body.message
    }
  } catch {
    // Response body wasn't JSON (or had no message) — fall back below.
  }

  return fallback
}

export async function fetchProfessionalProfile(): Promise<ProfessionalProfileResponse | null> {
  const { data, response } = await apiClient.GET('/api/professional-profile')
  if (response.status === 404) {
    return null
  }

  if (!response.ok || !data) {
    throw new Error(await describeError(response, `Could not load your professional profile (status ${response.status}).`))
  }

  return data
}

export async function createProfessionalProfile(contactInfo: ContactInfoDto, summaryMarkdown: string): Promise<boolean> {
  const headers = await csrfHeaders()
  const { response } = await apiClient.POST('/api/professional-profile', { headers, body: { contactInfo, summaryMarkdown } })
  if (response.status === 409) {
    return false
  }

  if (!response.ok) {
    throw new Error(await describeError(response, `Could not create your professional profile (status ${response.status}).`))
  }

  return true
}

export async function updateProfessionalProfile(contactInfo: ContactInfoDto, summaryMarkdown: string): Promise<void> {
  const headers = await csrfHeaders()
  const { response } = await apiClient.PUT('/api/professional-profile', { headers, body: { contactInfo, summaryMarkdown } })
  if (!response.ok) {
    throw new Error(await describeError(response, `Could not save your professional profile (status ${response.status}).`))
  }
}

export async function replaceExperience(experience: ExperienceEntryDto[]): Promise<void> {
  const headers = await csrfHeaders()
  const { response } = await apiClient.PUT('/api/professional-profile/experience', { headers, body: experience })
  if (!response.ok) {
    throw new Error(await describeError(response, `Could not save your experience (status ${response.status}).`))
  }
}

export async function replaceEducation(education: EducationEntryDto[]): Promise<void> {
  const headers = await csrfHeaders()
  const { response } = await apiClient.PUT('/api/professional-profile/education', { headers, body: education })
  if (!response.ok) {
    throw new Error(await describeError(response, `Could not save your education (status ${response.status}).`))
  }
}

export async function replaceSkills(skills: SkillDto[]): Promise<void> {
  const headers = await csrfHeaders()
  const { response } = await apiClient.PUT('/api/professional-profile/skills', { headers, body: skills })
  if (!response.ok) {
    throw new Error(await describeError(response, `Could not save your skills (status ${response.status}).`))
  }
}

export async function replaceLanguages(languages: LanguageEntryDto[]): Promise<void> {
  const headers = await csrfHeaders()
  const { response } = await apiClient.PUT('/api/professional-profile/languages', { headers, body: languages })
  if (!response.ok) {
    throw new Error(await describeError(response, `Could not save your languages (status ${response.status}).`))
  }
}

export async function replaceCertifications(certifications: CertificationEntryDto[]): Promise<void> {
  const headers = await csrfHeaders()
  const { response } = await apiClient.PUT('/api/professional-profile/certifications', { headers, body: certifications })
  if (!response.ok) {
    throw new Error(await describeError(response, `Could not save your certifications (status ${response.status}).`))
  }
}

export async function replaceProjects(projects: ProjectEntryDto[]): Promise<void> {
  const headers = await csrfHeaders()
  const { response } = await apiClient.PUT('/api/professional-profile/projects', { headers, body: projects })
  if (!response.ok) {
    throw new Error(await describeError(response, `Could not save your projects (status ${response.status}).`))
  }
}

export async function replaceProfileLinks(profileLinks: ProfileLinkDto[]): Promise<void> {
  const headers = await csrfHeaders()
  const { response } = await apiClient.PUT('/api/professional-profile/profile-links', { headers, body: profileLinks })
  if (!response.ok) {
    throw new Error(await describeError(response, `Could not save your profile links (status ${response.status}).`))
  }
}
