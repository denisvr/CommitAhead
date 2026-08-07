import { apiClient } from '../../api/client'
import type { components } from '../../api/generated/schema'

export type JobAnalysisResponse = components['schemas']['JobAnalysisResponse']
export type JobSourceResponse = components['schemas']['JobSourceResponse']
export type JobRequirementResponse = components['schemas']['JobRequirementResponse']
export type JobGapResponse = components['schemas']['JobGapResponse']
export type CreateJobAnalysisRequest = components['schemas']['CreateJobAnalysisRequest']
export type UpdateJobAnalysisRequest = components['schemas']['UpdateJobAnalysisRequest']

async function csrfHeaders(): Promise<{ 'X-CSRF-TOKEN': string }> {
  const { data } = await apiClient.GET('/auth/csrf')
  if (!data) {
    throw new Error('Could not obtain a CSRF token.')
  }

  return { 'X-CSRF-TOKEN': data.token }
}

// Unlike cv-presentations'/study-items' describeError, this also reads ProblemDetails' own
// `detail` field: DomainValidationExceptionFilter returns { title, detail } (not { message }),
// and a rejected PDF upload can fail for many distinct, backend-computed reasons (empty file,
// wrong MIME, malformed/encrypted/image-only/oversized PDF, too many pages, too much text) — each
// message is written by DomainValidationException specifically to be safe to show verbatim, so
// losing it behind one generic "could not create" string would throw away real information.
//
// Takes openapi-fetch's already-parsed `error` value rather than re-reading `response` itself:
// for a non-ok response, openapi-fetch's core fetch wrapper already consumes the body once via
// `response.text()` (and attempts its own JSON.parse) to populate this same value — the stream is
// already disturbed by the time a caller sees `response`, so `response.clone().json()` here would
// throw ("Body has already been consumed") and silently fall back every time.
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

export async function fetchJobAnalyses(): Promise<JobAnalysisResponse[]> {
  const { data, error, response } = await apiClient.GET('/api/job-analyses')
  if (!response.ok || !data) {
    throw new Error(describeError(error, `Could not load your job analyses (status ${response.status}).`))
  }

  return data
}

export async function fetchJobAnalysis(id: string): Promise<JobAnalysisResponse | null> {
  const { data, error, response } = await apiClient.GET('/api/job-analyses/{id}', { params: { path: { id } } })
  if (response.status === 404) {
    return null
  }

  if (!response.ok || !data) {
    throw new Error(describeError(error, `Could not load this job analysis (status ${response.status}).`))
  }

  return data
}

export async function createJobAnalysis(body: CreateJobAnalysisRequest): Promise<string> {
  const headers = await csrfHeaders()
  const { data, error, response } = await apiClient.POST('/api/job-analyses', { headers, body })
  if (!response.ok || !data) {
    throw new Error(describeError(error, `Could not create this job analysis (status ${response.status}).`))
  }

  return data.id
}

// The generated schema types this endpoint's request body as
// "application/x-www-form-urlencoded" (a known ASP.NET Core OpenAPI-generation quirk for
// [FromForm] + IFormFile — the real endpoint requires actual multipart/form-data, which can't
// carry a file any other way). openapi-fetch's own bodySerializer already passes a real
// `FormData` instance through unchanged and skips setting Content-Type itself, so the browser
// sets the multipart boundary correctly — the type assertion below only works around the
// generated type being wrong, not the runtime behaviour.
export async function createJobAnalysisFromUpload(title: string, file: File, notesMarkdown: string | null): Promise<string> {
  const headers = await csrfHeaders()
  const formData = new FormData()
  formData.append('Title', title)
  if (notesMarkdown) {
    formData.append('NotesMarkdown', notesMarkdown)
  }
  formData.append('File', file)

  const { data, error, response } = await apiClient.POST('/api/job-analyses/upload', {
    headers,
    body: formData as never,
  })
  if (!response.ok || !data) {
    throw new Error(describeError(error, `Could not create this job analysis (status ${response.status}).`))
  }

  return data.id
}

export async function updateJobAnalysis(id: string, body: UpdateJobAnalysisRequest): Promise<void> {
  const headers = await csrfHeaders()
  const { error, response } = await apiClient.PUT('/api/job-analyses/{id}', { headers, params: { path: { id } }, body })
  if (!response.ok) {
    throw new Error(describeError(error, `Could not save this job analysis (status ${response.status}).`))
  }
}

export async function deleteJobAnalysis(id: string): Promise<void> {
  const headers = await csrfHeaders()
  const { error, response } = await apiClient.DELETE('/api/job-analyses/{id}', { headers, params: { path: { id } } })
  if (!response.ok) {
    throw new Error(describeError(error, `Could not delete this job analysis (status ${response.status}).`))
  }
}
