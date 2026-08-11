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
      proposedDetailsJson: JSON.stringify({ SummaryMarkdown: 'A summary of indexing.', KeyPoints: ['Point'], InterviewQuestions: ['Q?'], References: [] }),
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

const EMPTY_DRAFT = { ...DRAFT, id: 'draft-empty', suggestionProposals: [], linkProposals: [], studyItemProposals: [] }

const APPLIED_DRAFT = {
  ...DRAFT,
  status: 'Applied',
  appliedAtUtc: '2026-01-02T00:00:00Z',
  suggestionProposals: [
    {
      id: 's1',
      status: 'Accepted',
      proposedCommandType: 'AddJobRequirement',
      proposedPayloadJson: JSON.stringify({ Text: 'Know Postgres', Kind: 'Technical', Priority: 'Required', SourceExcerpt: 'excerpt' }),
      proposedAdvisoryMarkdown: null,
      acceptedCommandType: 'AddJobRequirement',
      acceptedPayloadJson: JSON.stringify({ Text: 'Know Postgres well', Kind: 'Technical', Priority: 'Required', SourceExcerpt: 'excerpt' }),
    },
  ],
  linkProposals: [],
  studyItemProposals: [],
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

  it('lets an empty draft be resolved via Apply, with no proposals to decide', async () => {
    server.use(http.get('/api/analysis-drafts/:id', () => HttpResponse.json(EMPTY_DRAFT)))
    server.use(http.post('/api/analysis-drafts/:id/apply', () => new HttpResponse(null, { status: 204 })))
    const onApplied = vi.fn()

    render(<AnalysisDraftReviewPage draftId="draft-empty" onApplied={onApplied} onBack={vi.fn()} />)
    await screen.findByText('Nothing to review')

    const applyButton = screen.getByRole('button', { name: 'Apply' })
    expect(applyButton).toBeEnabled()
    await userEvent.click(applyButton)

    expect(onApplied).toHaveBeenCalled()
  })

  it('discards a Pending draft after confirmation, resolving it without deciding any proposals', async () => {
    server.use(http.get('/api/analysis-drafts/:id', () => HttpResponse.json(EMPTY_DRAFT)))
    let discardCalled = false
    server.use(
      http.post('/api/analysis-drafts/:id/discard', () => {
        discardCalled = true
        return new HttpResponse(null, { status: 204 })
      }),
    )
    const onBack = vi.fn()

    render(<AnalysisDraftReviewPage draftId="draft-empty" onApplied={vi.fn()} onBack={onBack} />)
    await screen.findByText('Nothing to review')

    await userEvent.click(screen.getByRole('button', { name: 'Discard' }))
    await userEvent.click(screen.getByRole('button', { name: 'Yes, discard' }))

    expect(discardCalled).toBe(true)
    expect(onBack).toHaveBeenCalled()
  })

  it('shows the full immutable proposed fields for a structured suggestion before any decision', async () => {
    server.use(http.get('/api/analysis-drafts/:id', () => HttpResponse.json(APPLIED_DRAFT)))

    render(<AnalysisDraftReviewPage draftId="draft-applied" onApplied={vi.fn()} onBack={vi.fn()} />)

    expect(await screen.findByText('Know Postgres')).toBeInTheDocument()
    expect(screen.getAllByText('excerpt')).toHaveLength(2)
  })

  it('shows the full proposed details for a StudyItem proposal before any decision', async () => {
    server.use(http.get('/api/analysis-drafts/:id', () => HttpResponse.json(DRAFT)))

    render(<AnalysisDraftReviewPage draftId="draft-1" onApplied={vi.fn()} onBack={vi.fn()} />)

    expect(await screen.findByText('A summary of indexing.')).toBeInTheDocument()
    expect(screen.getByText('Point')).toBeInTheDocument()
    expect(screen.getByText('Q?')).toBeInTheDocument()
  })

  it('renders an Applied draft as read-only, with no Accept/Reject/Apply/Discard controls', async () => {
    server.use(http.get('/api/analysis-drafts/:id', () => HttpResponse.json(APPLIED_DRAFT)))

    render(<AnalysisDraftReviewPage draftId="draft-applied" onApplied={vi.fn()} onBack={vi.fn()} />)

    await screen.findByText('AddJobRequirement')
    expect(screen.getByText('Know Postgres well')).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Accept' })).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Reject' })).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Apply' })).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Discard' })).not.toBeInTheDocument()
  })
})
