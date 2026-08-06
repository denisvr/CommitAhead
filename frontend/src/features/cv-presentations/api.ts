import { apiClient } from '../../api/client'
import type { components } from '../../api/generated/schema'

export type CVPresentationResponse = components['schemas']['CVPresentationResponse']
export type CreateCVPresentationRequest = components['schemas']['CreateCVPresentationRequest']
export type UpdateCVPresentationRequest = components['schemas']['UpdateCVPresentationRequest']

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

export async function fetchCVPresentations(): Promise<CVPresentationResponse[]> {
  const { data, response } = await apiClient.GET('/api/cv-presentations')
  if (!response.ok || !data) {
    throw new Error(await describeError(response, `Could not load your CV presentations (status ${response.status}).`))
  }

  return data
}

export async function fetchCVPresentation(id: string): Promise<CVPresentationResponse | null> {
  const { data, response } = await apiClient.GET('/api/cv-presentations/{id}', { params: { path: { id } } })
  if (response.status === 404) {
    return null
  }

  if (!response.ok || !data) {
    throw new Error(await describeError(response, `Could not load this CV presentation (status ${response.status}).`))
  }

  return data
}

export async function createCVPresentation(body: CreateCVPresentationRequest): Promise<string | null> {
  const headers = await csrfHeaders()
  const { data, response } = await apiClient.POST('/api/cv-presentations', { headers, body })
  if (response.status === 422) {
    return null
  }

  if (!response.ok || !data) {
    throw new Error(await describeError(response, `Could not create this CV presentation (status ${response.status}).`))
  }

  return data.id
}

export async function updateCVPresentation(id: string, body: UpdateCVPresentationRequest): Promise<void> {
  const headers = await csrfHeaders()
  const { response } = await apiClient.PUT('/api/cv-presentations/{id}', { headers, params: { path: { id } }, body })
  if (!response.ok) {
    throw new Error(await describeError(response, `Could not save this CV presentation (status ${response.status}).`))
  }
}

export async function deleteCVPresentation(id: string): Promise<void> {
  const headers = await csrfHeaders()
  const { response } = await apiClient.DELETE('/api/cv-presentations/{id}', { headers, params: { path: { id } } })
  if (!response.ok) {
    throw new Error(await describeError(response, `Could not delete this CV presentation (status ${response.status}).`))
  }
}

export async function replaceExperienceSelections(id: string, entryIds: string[]): Promise<void> {
  const headers = await csrfHeaders()
  const { response } = await apiClient.PUT('/api/cv-presentations/{id}/experience-selections', { headers, params: { path: { id } }, body: entryIds })
  if (!response.ok) {
    throw new Error(await describeError(response, `Could not save the experience selection (status ${response.status}).`))
  }
}

export async function replaceEducationSelections(id: string, entryIds: string[]): Promise<void> {
  const headers = await csrfHeaders()
  const { response } = await apiClient.PUT('/api/cv-presentations/{id}/education-selections', { headers, params: { path: { id } }, body: entryIds })
  if (!response.ok) {
    throw new Error(await describeError(response, `Could not save the education selection (status ${response.status}).`))
  }
}

export async function replaceSkillSelections(id: string, entryIds: string[]): Promise<void> {
  const headers = await csrfHeaders()
  const { response } = await apiClient.PUT('/api/cv-presentations/{id}/skill-selections', { headers, params: { path: { id } }, body: entryIds })
  if (!response.ok) {
    throw new Error(await describeError(response, `Could not save the skill selection (status ${response.status}).`))
  }
}

export async function replaceLanguageSelections(id: string, entryIds: string[]): Promise<void> {
  const headers = await csrfHeaders()
  const { response } = await apiClient.PUT('/api/cv-presentations/{id}/language-selections', { headers, params: { path: { id } }, body: entryIds })
  if (!response.ok) {
    throw new Error(await describeError(response, `Could not save the language selection (status ${response.status}).`))
  }
}

export async function replaceCertificationSelections(id: string, entryIds: string[]): Promise<void> {
  const headers = await csrfHeaders()
  const { response } = await apiClient.PUT('/api/cv-presentations/{id}/certification-selections', { headers, params: { path: { id } }, body: entryIds })
  if (!response.ok) {
    throw new Error(await describeError(response, `Could not save the certification selection (status ${response.status}).`))
  }
}

export async function replaceProjectSelections(id: string, entryIds: string[]): Promise<void> {
  const headers = await csrfHeaders()
  const { response } = await apiClient.PUT('/api/cv-presentations/{id}/project-selections', { headers, params: { path: { id } }, body: entryIds })
  if (!response.ok) {
    throw new Error(await describeError(response, `Could not save the project selection (status ${response.status}).`))
  }
}

export async function replaceProfileLinkSelections(id: string, entryIds: string[]): Promise<void> {
  const headers = await csrfHeaders()
  const { response } = await apiClient.PUT('/api/cv-presentations/{id}/profile-link-selections', { headers, params: { path: { id } }, body: entryIds })
  if (!response.ok) {
    throw new Error(await describeError(response, `Could not save the profile-link selection (status ${response.status}).`))
  }
}
