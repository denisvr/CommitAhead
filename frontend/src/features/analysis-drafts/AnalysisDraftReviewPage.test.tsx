import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { server } from '../../mocks/server'
import { AnalysisDraftReviewPage } from './AnalysisDraftReviewPage'

const DRAFT = {
  id: 'draft-1',
  sourceType: 'JobAnalysis',
  sourceId: 'source-1',
  status: 'Pending',
  createdAtUtc: '2026-01-01T00:00:00Z',
  appliedAtUtc: null,
  discardedAtUtc: null,
  suggestionProposals: [
    {
      id: 's1',
      status: 'Pending',
      proposedCommandType: null,
      proposedPayloadJson: null,
      proposedAdvisoryMarkdown: 'Consider highlighting your PostgreSQL experience.',
      acceptedCommandType: null,
      acceptedPayloadJson: null,
    },
  ],
  linkProposals: [
    {
      id: 'l1',
      status: 'Pending',
      targetStudyItemId: 'item-1',
      proposedWeight: 3,
      proposedRationale: 'Directly demonstrates this skill.',
      acceptedWeight: null,
      acceptedRationale: null,
    },
  ],
  studyItemProposals: [
    {
      id: 'si1',
      status: 'Pending',
      proposedTitle: 'PostgreSQL Indexing',
      proposedCategory: 'Theory',
      proposedDetailsJson: JSON.stringify({ SummaryMarkdown: 'Summary', KeyPoints: ['Point'], InterviewQuestions: ['Q?'], References: [] }),
      proposedTags: ['databases'],
      proposedImportance: 3,
      acceptedTitle: null,
      acceptedCategory: null,
      acceptedDetailsJson: null,
      acceptedTags: null,
      acceptedImportance: null,
      acceptedInitialMastery: null,
    },
  ],
}

describe('AnalysisDraftReviewPage', () => {
  it('shows a not-found message for a missing draft', async () => {
    server.use(http.get('/api/analysis-drafts/:id', () => new HttpResponse(null, { status: 404 })))

    render(<AnalysisDraftReviewPage draftId="missing" onApplied={vi.fn()} onBack={vi.fn()} />)

    expect(await screen.findByText('This analysis draft could not be found.')).toBeInTheDocument()
  })

  it('disables Apply until every proposal has a decision', async () => {
    server.use(http.get('/api/analysis-drafts/:id', () => HttpResponse.json(DRAFT)))

    render(<AnalysisDraftReviewPage draftId="draft-1" onApplied={vi.fn()} onBack={vi.fn()} />)
    await screen.findByText(/Consider highlighting/)

    expect(screen.getByRole('button', { name: 'Apply' })).toBeDisabled()

    const acceptButtons = screen.getAllByRole('button', { name: 'Accept' })
    for (const button of acceptButtons) {
      await userEvent.click(button)
    }

    expect(screen.getByRole('button', { name: 'Apply' })).toBeEnabled()
  })

  it('applies with accepted decisions built from the (possibly edited) proposed values', async () => {
    server.use(http.get('/api/analysis-drafts/:id', () => HttpResponse.json(DRAFT)))
    let requestBody: Record<string, unknown> | undefined
    server.use(
      http.post('/api/analysis-drafts/:id/apply', async ({ request }) => {
        requestBody = (await request.json()) as Record<string, unknown>
        return new HttpResponse(null, { status: 204 })
      }),
    )
    const onApplied = vi.fn()

    render(<AnalysisDraftReviewPage draftId="draft-1" onApplied={onApplied} onBack={vi.fn()} />)
    await screen.findByText(/Consider highlighting/)

    // Accept the advisory suggestion, the link proposal, and reject the study item proposal.
    const acceptButtons = screen.getAllByRole('button', { name: 'Accept' })
    await userEvent.click(acceptButtons[0])
    await userEvent.click(acceptButtons[1])
    const rejectButtons = screen.getAllByRole('button', { name: 'Reject' })
    await userEvent.click(rejectButtons[2])

    await userEvent.click(screen.getByRole('button', { name: 'Apply' }))

    expect(onApplied).toHaveBeenCalled()
    expect(requestBody?.suggestionDecisions).toEqual([{ proposalId: 's1', accepted: true, acceptedPayloadJson: null }])
    expect(requestBody?.linkDecisions).toEqual([{ proposalId: 'l1', accepted: true, weight: 3, rationale: 'Directly demonstrates this skill.' }])
    expect(requestBody?.studyItemDecisions).toEqual([
      { proposalId: 'si1', accepted: false, title: null, category: null, detailsJson: null, tags: null, importance: null, initialMastery: null },
    ])
  })
})
