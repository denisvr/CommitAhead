import { apiClient } from '../../api/client'
import type { components } from '../../api/generated/schema'

export type StudyItemCategory = components['schemas']['StudyItemCategory']
export type StudyItemStatus = components['schemas']['StudyItemStatus']
export type Difficulty = components['schemas']['Difficulty']
export type StudyItemDetailsDto = components['schemas']['StudyItemDetailsDto']
export type StudyItemResponse = components['schemas']['StudyItemResponse']
export type RankedStudyItemResponse = components['schemas']['RankedStudyItemResponse']
export type ScoringConfigResponse = components['schemas']['ScoringConfigResponse']
export type StudyReviewResponse = components['schemas']['StudyReviewResponse']
export type CreateStudyItemRequest = components['schemas']['CreateStudyItemRequest']
export type UpdateStudyItemRequest = components['schemas']['UpdateStudyItemRequest']

// openapi-typescript widens .NET's numeric formats (int32/double) to `number | string` since
// JSON Schema's "format" annotation doesn't guarantee the wire value is never quoted — every
// response from this API always sends real JSON numbers, so this narrows back at the boundary.
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

export async function fetchStudyQueue(): Promise<RankedStudyItemResponse[]> {
  const { data, response } = await apiClient.GET('/api/study-queue')
  if (!response.ok || !data) {
    throw new Error(await describeError(response, `Could not load the study queue (status ${response.status}).`))
  }

  return data
}

export async function fetchStudyItem(id: string): Promise<StudyItemResponse | null> {
  const { data, response } = await apiClient.GET('/api/study-items/{id}', { params: { path: { id } } })
  if (response.status === 404) {
    return null
  }

  if (!response.ok || !data) {
    throw new Error(await describeError(response, `Could not load this study item (status ${response.status}).`))
  }

  return data
}

export async function createStudyItem(body: CreateStudyItemRequest): Promise<string> {
  const headers = await csrfHeaders()
  const { data, response } = await apiClient.POST('/api/study-items', { headers, body })
  if (!response.ok || !data) {
    throw new Error(await describeError(response, `Could not create the study item (status ${response.status}).`))
  }

  return data.id
}

export async function updateStudyItem(id: string, body: UpdateStudyItemRequest): Promise<void> {
  const headers = await csrfHeaders()
  const { response } = await apiClient.PUT('/api/study-items/{id}', { headers, params: { path: { id } }, body })
  if (!response.ok) {
    throw new Error(await describeError(response, `Could not update this study item (status ${response.status}).`))
  }
}

export async function archiveStudyItem(id: string): Promise<void> {
  const headers = await csrfHeaders()
  const { response } = await apiClient.POST('/api/study-items/{id}/archive', { headers, params: { path: { id } } })
  if (!response.ok) {
    throw new Error(await describeError(response, `Could not archive this study item (status ${response.status}).`))
  }
}

export async function deleteStudyItem(id: string): Promise<void> {
  const headers = await csrfHeaders()
  const { response } = await apiClient.DELETE('/api/study-items/{id}', { headers, params: { path: { id } } })
  if (!response.ok) {
    throw new Error(await describeError(response, `Could not delete this study item (status ${response.status}).`))
  }
}

export async function submitStudyReview(id: string, confidenceRating: number, notesMarkdown: string | null): Promise<void> {
  const headers = await csrfHeaders()
  const { response } = await apiClient.POST('/api/study-items/{id}/reviews', {
    headers,
    params: { path: { id } },
    body: { confidenceRating, notesMarkdown },
  })
  if (!response.ok) {
    throw new Error(await describeError(response, `Could not save this review (status ${response.status}).`))
  }
}

export async function setPriorityOverride(id: string, score: number, reason: string): Promise<void> {
  const headers = await csrfHeaders()
  const { response } = await apiClient.PUT('/api/study-items/{id}/priority-override', {
    headers,
    params: { path: { id } },
    body: { score, reason },
  })
  if (!response.ok) {
    throw new Error(await describeError(response, `Could not set the priority override (status ${response.status}).`))
  }
}

export async function clearPriorityOverride(id: string): Promise<void> {
  const headers = await csrfHeaders()
  const { response } = await apiClient.DELETE('/api/study-items/{id}/priority-override', { headers, params: { path: { id } } })
  if (!response.ok) {
    throw new Error(await describeError(response, `Could not clear the priority override (status ${response.status}).`))
  }
}

export async function fetchScoringConfig(): Promise<ScoringConfigResponse> {
  const { data, response } = await apiClient.GET('/api/scoring-config')
  if (!response.ok || !data) {
    throw new Error(await describeError(response, `Could not load scoring settings (status ${response.status}).`))
  }

  return data
}

export async function updateScoringConfig(importanceWeight: number, demandWeight: number, masteryGapWeight: number): Promise<void> {
  const headers = await csrfHeaders()
  const { response } = await apiClient.PUT('/api/scoring-config', {
    headers,
    body: { importanceWeight, demandWeight, masteryGapWeight },
  })
  if (!response.ok) {
    throw new Error(await describeError(response, `Could not save scoring settings (status ${response.status}).`))
  }
}

export async function resetScoringConfig(): Promise<void> {
  const headers = await csrfHeaders()
  const { response } = await apiClient.DELETE('/api/scoring-config', { headers })
  if (!response.ok) {
    throw new Error(await describeError(response, `Could not reset scoring settings (status ${response.status}).`))
  }
}
