import { useCallback, useEffect, useState } from 'react'
import { Button } from '../../design-system/components/Button'
import { Chip } from '../../design-system/components/Chip'
import { EmptyState } from '../../design-system/components/EmptyState'
import { Icon } from '../../design-system/Icon'
import { deleteInterviewNote, fetchInterviewNote, toNumber, type InterviewNoteResponse } from './api'
import { InterviewNoteForm } from './InterviewNoteForm'
import styles from './InterviewNoteDetailPage.module.css'

type LoadState = 'loading' | 'ready' | 'not-found' | 'error'

type InterviewNoteDetailPageProps = {
  noteId: string
  onBack: () => void
  onDeleted: () => void
}

function describeError(caught: unknown, fallback: string): string {
  return caught instanceof Error ? caught.message : fallback
}

function formatDate(isoDate: string): string {
  const [year, month, day] = isoDate.split('-').map(Number)
  return new Date(year, month - 1, day).toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric' })
}

// components.md AppShell destination 5.
export function InterviewNoteDetailPage({ noteId, onBack, onDeleted }: InterviewNoteDetailPageProps) {
  const [loadState, setLoadState] = useState<LoadState>('loading')
  const [note, setNote] = useState<InterviewNoteResponse | null>(null)
  const [loadError, setLoadError] = useState<string | null>(null)
  const [isEditing, setIsEditing] = useState(false)
  const [confirmingDelete, setConfirmingDelete] = useState(false)
  const [actionError, setActionError] = useState<string | null>(null)
  const [isBusy, setIsBusy] = useState(false)

  const load = useCallback(async () => {
    try {
      const data = await fetchInterviewNote(noteId)
      if (!data) {
        setLoadState('not-found')
        return
      }

      setNote(data)
      setLoadState('ready')
    } catch (caught) {
      setLoadError(describeError(caught, 'Something went wrong loading this interview note.'))
      setLoadState('error')
    }
  }, [noteId])

  // Inlined rather than calling load() directly — see StudyItemDetailPage for why.
  useEffect(() => {
    fetchInterviewNote(noteId)
      .then((data) => {
        if (!data) {
          setLoadState('not-found')
          return
        }

        setNote(data)
        setLoadState('ready')
      })
      .catch((caught: unknown) => {
        setLoadError(describeError(caught, 'Something went wrong loading this interview note.'))
        setLoadState('error')
      })
  }, [noteId])

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
        <p>This interview note could not be found.</p>
        <Button onClick={onBack}>Back to interview notes</Button>
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

  const data = note!

  if (isEditing) {
    return (
      <InterviewNoteForm
        mode="edit"
        note={data}
        onSaved={() => {
          setIsEditing(false)
          void load()
        }}
        onCancel={() => setIsEditing(false)}
      />
    )
  }

  const handleDelete = async () => {
    setIsBusy(true)
    setActionError(null)
    try {
      await deleteInterviewNote(data.id)
      onDeleted()
    } catch (caught) {
      setActionError(describeError(caught, 'Something went wrong deleting this interview note.'))
      setIsBusy(false)
      setConfirmingDelete(false)
    }
  }

  return (
    <div className={styles.page}>
      <Button variant="ghost" className={styles.back} onClick={onBack}>
        Back to interview notes
      </Button>

      <header className={styles.header}>
        <div className={styles.titleGroup}>
          <h1 className={styles.title}>
            {data.company} — {data.role}
          </h1>
          <p className={styles.meta}>
            <span>{data.interviewRound === 'Other' ? data.otherLabel : data.interviewRound}</span>
            <span>·</span>
            <span>Round #{toNumber(data.sequenceNumber)}</span>
            <span>·</span>
            <span>{formatDate(data.date)}</span>
          </p>
        </div>
        <div className={styles.actions}>
          <Button variant="secondary" onClick={() => setIsEditing(true)}>
            <Icon name="pencil" /> Edit
          </Button>
          {confirmingDelete ? (
            <span className={styles.confirmRow}>
              <span>Delete this interview note permanently?</span>
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

      {data.jobAnalysisId && (
        <section className={styles.section} aria-label="Linked job analysis">
          <Chip>Linked to a job analysis</Chip>
        </section>
      )}

      <section className={styles.section} aria-label="Questions asked">
        <h2 className={styles.sectionTitle}>Questions asked</h2>
        {data.questions.length === 0 ? <EmptyState title="No questions recorded" description="Nothing recorded for this round." /> : <EntryList items={data.questions} />}
      </section>

      <section className={styles.section} aria-label="Gaps observed">
        <h2 className={styles.sectionTitle}>Gaps observed</h2>
        {data.gaps.length === 0 ? <EmptyState title="No gaps recorded" description="Nothing recorded for this round." /> : <EntryList items={data.gaps} />}
      </section>

      <section className={styles.section} aria-label="Lessons learned">
        <h2 className={styles.sectionTitle}>Lessons learned</h2>
        {data.lessons.length === 0 ? <EmptyState title="No lessons recorded" description="Nothing recorded for this round." /> : <EntryList items={data.lessons} />}
      </section>
    </div>
  )
}

function EntryList({ items }: { items: string[] }) {
  return (
    <ul className={styles.entryList}>
      {items.map((item, index) => (
        <li key={`${index}-${item}`} className={styles.entryItem}>
          {item}
        </li>
      ))}
    </ul>
  )
}
