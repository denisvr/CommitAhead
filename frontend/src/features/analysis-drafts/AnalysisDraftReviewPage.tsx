import { useEffect, useState } from 'react'
import { Button } from '../../design-system/components/Button'
import { EmptyState } from '../../design-system/components/EmptyState'
import {
  applyAnalysisDraft,
  fetchAnalysisDraft,
  type AnalysisDraftResponse,
  type ApplyAnalysisDraftRequest,
  type LinkProposalDecision,
  type StudyItemProposalDecision,
  type SuggestionProposalDecision,
} from './api'
import { LinkProposalCard, type LinkDecisionState } from './LinkProposalCard'
import {
  buildStudyItemDetailsJson,
  buildSuggestionPayloadJson,
  parseStudyItemDetailsFields,
  parseSuggestionFields,
} from './payloadFields'
import { StudyItemProposalCard, type StudyItemDecisionState } from './StudyItemProposalCard'
import { SuggestionProposalCard, type SuggestionDecisionState } from './SuggestionProposalCard'
import styles from './AnalysisDraftReviewPage.module.css'

type LoadState = 'loading' | 'ready' | 'not-found' | 'error'

type AnalysisDraftReviewPageProps = {
  draftId: string
  onApplied: () => void
  onBack: () => void
}

function describeError(caught: unknown, fallback: string): string {
  return caught instanceof Error ? caught.message : fallback
}

function initialSuggestionDecision(proposal: AnalysisDraftResponse['suggestionProposals'][number]): SuggestionDecisionState {
  return {
    decided: false,
    accepted: false,
    fields: proposal.proposedCommandType && proposal.proposedPayloadJson ? parseSuggestionFields(proposal.proposedCommandType, proposal.proposedPayloadJson) : {},
  }
}

function initialLinkDecision(proposal: AnalysisDraftResponse['linkProposals'][number]): LinkDecisionState {
  return { decided: false, accepted: false, weight: String(proposal.proposedWeight), rationale: proposal.proposedRationale }
}

function initialStudyItemDecision(proposal: AnalysisDraftResponse['studyItemProposals'][number]): StudyItemDecisionState {
  return {
    decided: false,
    accepted: false,
    title: proposal.proposedTitle,
    detailsFields: parseStudyItemDetailsFields(proposal.proposedCategory, proposal.proposedDetailsJson),
    tags: [...proposal.proposedTags],
    importance: Number(proposal.proposedImportance),
    initialMastery: 3,
  }
}

// components.md "AI analysis draft": every proposal requires exactly one Accepted/Rejected
// decision before Apply is available; accepted actionable proposals expose their complete
// editable final payload.
export function AnalysisDraftReviewPage({ draftId, onApplied, onBack }: AnalysisDraftReviewPageProps) {
  const [loadState, setLoadState] = useState<LoadState>('loading')
  const [draft, setDraft] = useState<AnalysisDraftResponse | null>(null)
  const [loadError, setLoadError] = useState<string | null>(null)

  const [suggestionDecisions, setSuggestionDecisions] = useState<Record<string, SuggestionDecisionState>>({})
  const [linkDecisions, setLinkDecisions] = useState<Record<string, LinkDecisionState>>({})
  const [studyItemDecisions, setStudyItemDecisions] = useState<Record<string, StudyItemDecisionState>>({})

  const [isApplying, setIsApplying] = useState(false)
  const [applyError, setApplyError] = useState<string | null>(null)

  useEffect(() => {
    fetchAnalysisDraft(draftId)
      .then((data) => {
        if (!data) {
          setLoadState('not-found')
          return
        }

        setDraft(data)
        setSuggestionDecisions(Object.fromEntries(data.suggestionProposals.map((p) => [p.id, initialSuggestionDecision(p)])))
        setLinkDecisions(Object.fromEntries(data.linkProposals.map((p) => [p.id, initialLinkDecision(p)])))
        setStudyItemDecisions(Object.fromEntries(data.studyItemProposals.map((p) => [p.id, initialStudyItemDecision(p)])))
        setLoadState('ready')
      })
      .catch((caught: unknown) => {
        setLoadError(describeError(caught, 'Something went wrong loading this analysis draft.'))
        setLoadState('error')
      })
  }, [draftId])

  if (loadState === 'loading') {
    return (
      <p className={styles.status} role="status">
        Loading…
      </p>
    )
  }

  if (loadState === 'not-found') {
    return (
      <div className={styles.page}>
        <p>This analysis draft could not be found.</p>
        <Button onClick={onBack}>Back</Button>
      </div>
    )
  }

  if (loadState === 'error') {
    return (
      <div className={styles.page}>
        <p role="alert">{loadError}</p>
        <Button onClick={onBack}>Back</Button>
      </div>
    )
  }

  const data = draft!

  const allDecided =
    Object.values(suggestionDecisions).every((d) => d.decided) &&
    Object.values(linkDecisions).every((d) => d.decided) &&
    Object.values(studyItemDecisions).every((d) => d.decided)

  const handleApply = async () => {
    setIsApplying(true)
    setApplyError(null)

    try {
      const suggestionRequest: SuggestionProposalDecision[] = data.suggestionProposals.map((proposal) => {
        const decision = suggestionDecisions[proposal.id]
        return {
          proposalId: proposal.id,
          accepted: decision.accepted,
          acceptedPayloadJson:
            decision.accepted && proposal.proposedCommandType ? buildSuggestionPayloadJson(proposal.proposedCommandType, decision.fields) : null,
        }
      })

      const linkRequest: LinkProposalDecision[] = data.linkProposals.map((proposal) => {
        const decision = linkDecisions[proposal.id]
        return {
          proposalId: proposal.id,
          accepted: decision.accepted,
          weight: decision.accepted ? Number(decision.weight) : null,
          rationale: decision.accepted ? decision.rationale : null,
        }
      })

      const studyItemRequest: StudyItemProposalDecision[] = data.studyItemProposals.map((proposal) => {
        const decision = studyItemDecisions[proposal.id]
        return {
          proposalId: proposal.id,
          accepted: decision.accepted,
          title: decision.accepted ? decision.title : null,
          category: decision.accepted ? proposal.proposedCategory : null,
          detailsJson: decision.accepted ? buildStudyItemDetailsJson(proposal.proposedCategory, decision.detailsFields) : null,
          tags: decision.accepted ? decision.tags : null,
          importance: decision.accepted ? decision.importance : null,
          initialMastery: decision.accepted ? decision.initialMastery : null,
        }
      })

      const request: ApplyAnalysisDraftRequest = { suggestionDecisions: suggestionRequest, linkDecisions: linkRequest, studyItemDecisions: studyItemRequest }
      await applyAnalysisDraft(draftId, request)
      onApplied()
    } catch (caught) {
      setApplyError(describeError(caught, 'Something went wrong applying this analysis draft.'))
    } finally {
      setIsApplying(false)
    }
  }

  const hasNoProposals = data.suggestionProposals.length === 0 && data.linkProposals.length === 0 && data.studyItemProposals.length === 0

  return (
    <div className={styles.page}>
      <Button variant="ghost" className={styles.back} onClick={onBack}>
        Back
      </Button>

      <header className={styles.header}>
        <h1 className={styles.title}>Review analysis draft</h1>
      </header>

      {applyError && <p role="alert">{applyError}</p>}

      {hasNoProposals && <EmptyState title="Nothing to review" description="This analysis produced no proposals." />}

      {data.suggestionProposals.length > 0 && (
        <section className={styles.section} aria-label="Suggestion proposals">
          <h2 className={styles.sectionTitle}>Suggestions</h2>
          <ul className={styles.list}>
            {data.suggestionProposals.map((proposal) => (
              <SuggestionProposalCard
                key={proposal.id}
                proposal={proposal}
                decision={suggestionDecisions[proposal.id]}
                onChange={(decision) => setSuggestionDecisions((prev) => ({ ...prev, [proposal.id]: decision }))}
              />
            ))}
          </ul>
        </section>
      )}

      {data.linkProposals.length > 0 && (
        <section className={styles.section} aria-label="Link proposals">
          <h2 className={styles.sectionTitle}>Study item links</h2>
          <ul className={styles.list}>
            {data.linkProposals.map((proposal) => (
              <LinkProposalCard
                key={proposal.id}
                proposal={proposal}
                decision={linkDecisions[proposal.id]}
                onChange={(decision) => setLinkDecisions((prev) => ({ ...prev, [proposal.id]: decision }))}
              />
            ))}
          </ul>
        </section>
      )}

      {data.studyItemProposals.length > 0 && (
        <section className={styles.section} aria-label="Study item proposals">
          <h2 className={styles.sectionTitle}>New study items</h2>
          <ul className={styles.list}>
            {data.studyItemProposals.map((proposal) => (
              <StudyItemProposalCard
                key={proposal.id}
                proposal={proposal}
                decision={studyItemDecisions[proposal.id]}
                onChange={(decision) => setStudyItemDecisions((prev) => ({ ...prev, [proposal.id]: decision }))}
              />
            ))}
          </ul>
        </section>
      )}

      {!hasNoProposals && (
        <Button variant="primary" onClick={handleApply} disabled={!allDecided} isLoading={isApplying}>
          Apply
        </Button>
      )}
    </div>
  )
}
