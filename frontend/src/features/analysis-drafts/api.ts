import { apiClient } from '../../api/client'
import type { components } from '../../api/generated/schema'

export type AnalysisDraftResponse = components['schemas']['AnalysisDraftResponse']
export type SuggestionProposalResponse = components['schemas']['SuggestionProposalResponse']
export type LinkProposalResponse = components['schemas']['LinkProposalResponse']
export type StudyItemProposalResponse = components['schemas']['StudyItemProposalResponse']
export type ApplyAnalysisDraftRequest = components['schemas']['ApplyAnalysisDraftRequest']
export type SuggestionProposalDecision = components['schemas']['SuggestionProposalDecision']
export type LinkProposalDecision = components['schemas']['LinkProposalDecision']
export type StudyItemProposalDecision = components['schemas']['StudyItemProposalDecision']

async function csrfHeaders(): Promise<{ 'X-CSRF-TOKEN': string }> {
  const { data } = await apiClient.GET('/auth/csrf')
  if (!data) {
    throw new Error('Could not obtain a CSRF token.')
  }

  return { 'X-CSRF-TOKEN': data.token }
}

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

export async function fetchAnalysisDraft(id: string): Promise<AnalysisDraftResponse | null> {
  const { data, error, response } = await apiClient.GET('/api/analysis-drafts/{id}', { params: { path: { id } } })
  if (response.status === 404) {
    return null
  }

  if (!response.ok || !data) {
    throw new Error(describeError(error, `Could not load this analysis draft (status ${response.status}).`))
  }

  return data
}

export async function applyAnalysisDraft(id: string, body: ApplyAnalysisDraftRequest): Promise<void> {
  const headers = await csrfHeaders()
  const { error, response } = await apiClient.POST('/api/analysis-drafts/{id}/apply', { headers, params: { path: { id } }, body })
  if (!response.ok) {
    throw new Error(describeError(error, `Could not apply this analysis draft (status ${response.status}).`))
  }
}

export async function discardAnalysisDraft(id: string): Promise<void> {
  const headers = await csrfHeaders()
  const { error, response } = await apiClient.POST('/api/analysis-drafts/{id}/discard', { headers, params: { path: { id } } })
  if (!response.ok) {
    throw new Error(describeError(error, `Could not discard this analysis draft (status ${response.status}).`))
  }
}
