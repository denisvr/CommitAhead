import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { Button } from '../../design-system/components/Button'
import { Chip } from '../../design-system/components/Chip'
import { EmptyState } from '../../design-system/components/EmptyState'
import { Field } from '../../design-system/components/Field'
import { Icon } from '../../design-system/Icon'
import { RestrictedMarkdown } from '../../design-system/components/RestrictedMarkdown'
import inputStyles from '../../design-system/components/Input.module.css'
import { analyzeJobAnalysis, deleteJobAnalysis, fetchJobAnalysis, updateJobAnalysis, type JobAnalysisResponse } from './api'
import layout from './FormLayout.module.css'
import styles from './JobAnalysisDetailPage.module.css'

type LoadState = 'loading' | 'ready' | 'not-found' | 'error'

type JobAnalysisDetailPageProps = {
  analysisId: string
  onBack: () => void
  onDeleted: () => void
  onAnalyzed: (draftId: string) => void
}

function describeError(caught: unknown, fallback: string): string {
  return caught instanceof Error ? caught.message : fallback
}

function describeBlockedAnalyzeOutcome(outcomeCode: string): string {
  switch (outcomeCode) {
    case 'InProgress':
      return 'An analysis is already running for this job analysis.'
    case 'AnotherAnalysisInProgress':
      return 'Another analysis is already in progress for your account — try again once it finishes.'
    case 'DraftAlreadyPending':
      return 'An analysis draft is already pending review for this job analysis.'
    case 'DailyBudgetExceeded':
      return "Today's AI usage budget has been reached — try again tomorrow."
    case 'MonthlyBudgetExceeded':
      return "This month's AI usage budget has been reached."
    case 'FailedPreviously':
      return 'The previous analysis attempt failed — try again.'
    default:
      return 'Something went wrong starting the analysis.'
  }
}

function EditJobAnalysisForm({ analysis, onSaved, onCancel }: { analysis: JobAnalysisResponse; onSaved: () => void; onCancel: () => void }) {
  const [title, setTitle] = useState(analysis.title)
  const [notesMarkdown, setNotesMarkdown] = useState(analysis.notesMarkdown ?? '')
  const [error, setError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setIsSubmitting(true)
    setError(null)

    try {
      await updateJobAnalysis(analysis.id, { title, notesMarkdown: notesMarkdown || null })
      onSaved()
    } catch (caught) {
      setError(describeError(caught, 'Something went wrong saving this job analysis.'))
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <form className={layout.stack} onSubmit={handleSubmit}>
      <Field label="Title">
        {(fieldProps) => <input {...fieldProps} type="text" required className={inputStyles.input} value={title} onChange={(event) => setTitle(event.target.value)} />}
      </Field>

      <Field label="Notes" hint="Optional. The job posting text itself can't be edited here — create a new analysis for a different posting.">
        {(fieldProps) => (
          <textarea {...fieldProps} rows={4} className={inputStyles.input} value={notesMarkdown} onChange={(event) => setNotesMarkdown(event.target.value)} />
        )}
      </Field>

      {error && <p role="alert">{error}</p>}

      <div className={layout.row}>
        <Button type="submit" variant="primary" isLoading={isSubmitting}>
          Save
        </Button>
        <Button type="button" variant="ghost" onClick={onCancel}>
          Cancel
        </Button>
      </div>
    </form>
  )
}

// components.md AppShell destination 4. JobSource is immutable after creation (JobAnalysis
// domain invariant) — editing here only ever touches title/notes, matching
// UpdateJobAnalysisRequest's own shape.
export function JobAnalysisDetailPage({ analysisId, onBack, onDeleted, onAnalyzed }: JobAnalysisDetailPageProps) {
  const [loadState, setLoadState] = useState<LoadState>('loading')
  const [analysis, setAnalysis] = useState<JobAnalysisResponse | null>(null)
  const [loadError, setLoadError] = useState<string | null>(null)
  const [isEditing, setIsEditing] = useState(false)
  const [confirmingDelete, setConfirmingDelete] = useState(false)
  const [actionError, setActionError] = useState<string | null>(null)
  const [isBusy, setIsBusy] = useState(false)
  const [isAnalyzing, setIsAnalyzing] = useState(false)

  const load = useCallback(async () => {
    try {
      const data = await fetchJobAnalysis(analysisId)
      if (!data) {
        setLoadState('not-found')
        return
      }

      setAnalysis(data)
      setLoadState('ready')
    } catch (caught) {
      setLoadError(describeError(caught, 'Something went wrong loading this job analysis.'))
      setLoadState('error')
    }
  }, [analysisId])

  // Inlined rather than calling load() directly — see StudyItemDetailPage for why (the set-state-
  // in-effect lint rule treats any call to a state-setting function reference as synchronous).
  useEffect(() => {
    fetchJobAnalysis(analysisId)
      .then((data) => {
        if (!data) {
          setLoadState('not-found')
          return
        }

        setAnalysis(data)
        setLoadState('ready')
      })
      .catch((caught: unknown) => {
        setLoadError(describeError(caught, 'Something went wrong loading this job analysis.'))
        setLoadState('error')
      })
  }, [analysisId])

  const retry = () => {
    setLoadState('loading')
    void load()
  }

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
        <p>This job analysis could not be found.</p>
        <Button onClick={onBack}>Back to job analyses</Button>
      </div>
    )
  }

  if (loadState === 'error') {
    return (
      <div className={styles.page}>
        <p role="alert">{loadError}</p>
        <Button onClick={retry}>Try again</Button>
      </div>
    )
  }

  const data = analysis!

  if (isEditing) {
    return (
      <EditJobAnalysisForm
        analysis={data}
        onSaved={() => {
          setIsEditing(false)
          void load()
        }}
        onCancel={() => setIsEditing(false)}
      />
    )
  }

  const handleAnalyze = async () => {
    setIsAnalyzing(true)
    setActionError(null)
    try {
      const result = await analyzeJobAnalysis(data.id, crypto.randomUUID())
      if (result.kind === 'started') {
        onAnalyzed(result.analysisDraftId)
        return
      }

      setActionError(result.kind === 'sourceNotFound' ? 'This job analysis could not be found.' : describeBlockedAnalyzeOutcome(result.outcomeCode))
    } catch (caught) {
      setActionError(describeError(caught, 'Something went wrong analyzing this job analysis.'))
    } finally {
      setIsAnalyzing(false)
    }
  }

  const handleDelete = async () => {
    setIsBusy(true)
    setActionError(null)
    try {
      await deleteJobAnalysis(data.id)
      onDeleted()
    } catch (caught) {
      setActionError(describeError(caught, 'Something went wrong deleting this job analysis.'))
      setIsBusy(false)
      setConfirmingDelete(false)
    }
  }

  return (
    <div className={styles.page}>
      <Button variant="ghost" className={styles.back} onClick={onBack}>
        Back to job analyses
      </Button>

      <header className={styles.header}>
        <h1 className={styles.title}>{data.title}</h1>
        <div className={styles.actions}>
          <Button variant="primary" onClick={handleAnalyze} isLoading={isAnalyzing}>
            Analyze
          </Button>
          <Button variant="secondary" onClick={() => setIsEditing(true)}>
            <Icon name="pencil" /> Edit
          </Button>
          {confirmingDelete ? (
            <span className={styles.confirmRow}>
              <span>Delete this job analysis permanently?</span>
              <Button variant="danger" onClick={handleDelete} isLoading={isBusy}>
                Yes, delete
              </Button>
              <Button variant="ghost" onClick={() => setConfirmingDelete(false)}>
                Cancel
              </Button>
            </span>
          ) : (
            <Button variant="danger" onClick={() => setConfirmingDelete(true)}>
              <Icon name="trash-2" /> Delete
            </Button>
          )}
        </div>
      </header>

      {actionError && <p role="alert">{actionError}</p>}

      <section className={styles.section} aria-label="Job posting source">
        <h2 className={styles.sectionTitle}>Job posting source</h2>
        {/* `kind` is generated as optional, so it doesn't narrow the union on its own — checking
            for a field unique to PastedText does. */}
        {'content' in data.jobSource ? (
          <>
            <Chip>Pasted text</Chip>
            <p className={styles.sourceText}>{data.jobSource.content}</p>
          </>
        ) : (
          <>
            <Chip>Uploaded PDF — {data.jobSource.originalFileName}</Chip>
            <p className={styles.hint}>Extracted text, for verification — this is exactly what analysis will read:</p>
            <p className={styles.sourceText}>{data.jobSource.extractedText}</p>
          </>
        )}
      </section>

      <section className={styles.section} aria-label="Notes">
        <h2 className={styles.sectionTitle}>Notes</h2>
        {data.notesMarkdown ? <RestrictedMarkdown>{data.notesMarkdown}</RestrictedMarkdown> : <p className={styles.status}>No notes.</p>}
      </section>

      <section className={styles.section} aria-label="Requirements">
        <h2 className={styles.sectionTitle}>Requirements</h2>
        {data.requirements.length === 0 ? (
          <EmptyState title="No requirements yet" description="Requirements are identified by AI analysis, not added here directly." />
        ) : (
          <ul className={styles.entryList}>
            {data.requirements.map((requirement) => (
              <li key={requirement.id} className={styles.entryItem}>
                <span className={styles.entryMeta}>
                  {requirement.kind} · {requirement.priority}
                </span>
                <span>{requirement.text}</span>
              </li>
            ))}
          </ul>
        )}
      </section>

      <section className={styles.section} aria-label="Gaps">
        <h2 className={styles.sectionTitle}>Gaps</h2>
        {data.gaps.length === 0 ? (
          <EmptyState title="No gaps yet" description="Gaps are identified by AI analysis, not added here directly." />
        ) : (
          <ul className={styles.entryList}>
            {data.gaps.map((gap) => (
              <li key={gap.id} className={styles.entryItem}>
                <span className={styles.entryMeta}>
                  {gap.matchLevel} · {gap.severity}
                </span>
                <span>{gap.rationale}</span>
              </li>
            ))}
          </ul>
        )}
      </section>
    </div>
  )
}
