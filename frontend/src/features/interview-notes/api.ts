import { apiClient } from '../../api/client'
import type { components } from '../../api/generated/schema'

export type InterviewNoteResponse = components['schemas']['InterviewNoteResponse']
export type InterviewRound = components['schemas']['InterviewRound']
export type CreateInterviewNoteRequest = components['schemas']['CreateInterviewNoteRequest']
export type UpdateInterviewNoteRequest = components['schemas']['UpdateInterviewNoteRequest']

// openapi-typescript widens .NET's int32 SequenceNumber to `number | string` — see study-items/
// api.ts's toNumber for why every response here always sends a real JSON number regardless.
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

// Also reads ProblemDetails' own `detail` field (see job-analyses/api.ts) — an invalid
// jobAnalysisId is a DomainValidationException whose message is specifically written to be safe
// to show verbatim, not a generic "could not save" string.
//
// Takes openapi-fetch's already-parsed `error` value rather than re-reading `response` itself —
// see job-analyses/api.ts's describeError for why re-reading the response body here would throw.
function describeError(error: unknown, fallback: string): string {
  if (error && typeof error === 'object') {
    const body = error as { message?: string; detail?: string }
    if (body.detail) {
      return body.detail
    }

    if (body.message) {
      return body.message
    }
  }

  return fallback
}

export async function fetchInterviewNotes(): Promise<InterviewNoteResponse[]> {
  const { data, error, response } = await apiClient.GET('/api/interview-notes')
  if (!response.ok || !data) {
    throw new Error(describeError(error, `Could not load your interview notes (status ${response.status}).`))
  }

  return data
}

export async function fetchInterviewNote(id: string): Promise<InterviewNoteResponse | null> {
  const { data, error, response } = await apiClient.GET('/api/interview-notes/{id}', { params: { path: { id } } })
  if (response.status === 404) {
    return null
  }

  if (!response.ok || !data) {
    throw new Error(describeError(error, `Could not load this interview note (status ${response.status}).`))
  }

  return data
}

export async function createInterviewNote(body: CreateInterviewNoteRequest): Promise<string> {
  const headers = await csrfHeaders()
  const { data, error, response } = await apiClient.POST('/api/interview-notes', { headers, body })
  if (!response.ok || !data) {
    throw new Error(describeError(error, `Could not create this interview note (status ${response.status}).`))
  }

  return data.id
}

export async function updateInterviewNote(id: string, body: UpdateInterviewNoteRequest): Promise<void> {
  const headers = await csrfHeaders()
  const { error, response } = await apiClient.PUT('/api/interview-notes/{id}', { headers, params: { path: { id } }, body })
  if (!response.ok) {
    throw new Error(describeError(error, `Could not save this interview note (status ${response.status}).`))
  }
}

export async function deleteInterviewNote(id: string): Promise<void> {
  const headers = await csrfHeaders()
  const { error, response } = await apiClient.DELETE('/api/interview-notes/{id}', { headers, params: { path: { id } } })
  if (!response.ok) {
    throw new Error(describeError(error, `Could not delete this interview note (status ${response.status}).`))
  }
}
