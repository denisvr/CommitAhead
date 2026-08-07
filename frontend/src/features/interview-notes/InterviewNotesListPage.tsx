import { useEffect, useState } from 'react'
import { Button } from '../../design-system/components/Button'
import { Chip } from '../../design-system/components/Chip'
import { EmptyState } from '../../design-system/components/EmptyState'
import { Icon } from '../../design-system/Icon'
import { fetchInterviewNotes, toNumber, type InterviewNoteResponse } from './api'
import styles from './InterviewNotesListPage.module.css'

type LoadState = 'loading' | 'ready' | 'error'

type InterviewNotesListPageProps = {
  onSelectNote: (id: string) => void
  onCreateNew: () => void
}

function formatDate(isoDate: string): string {
  // A plain "date" (no time component, no timezone conversion) — parse the y-m-d parts directly
  // rather than through the Date constructor, which would interpret a bare "yyyy-MM-dd" as UTC
  // midnight and could display the previous day in a timezone west of UTC.
  const [year, month, day] = isoDate.split('-').map(Number)
  return new Date(year, month - 1, day).toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric' })
}

// components.md AppShell destination 5, "Interview notes" — a flat list, like Job analyses.
export function InterviewNotesListPage({ onSelectNote, onCreateNew }: InterviewNotesListPageProps) {
  const [loadState, setLoadState] = useState<LoadState>('loading')
  const [notes, setNotes] = useState<InterviewNoteResponse[]>([])
  const [error, setError] = useState<string | null>(null)
  const [retryToken, setRetryToken] = useState(0)

  useEffect(() => {
    let stale = false

    fetchInterviewNotes()
      .then((data) => {
        if (stale) return
        setNotes(data)
        setLoadState('ready')
      })
      .catch((caught: unknown) => {
        if (stale) return
        setError(caught instanceof Error ? caught.message : 'Something went wrong loading your interview notes.')
        setLoadState('error')
      })

    return () => {
      stale = true
    }
  }, [retryToken])

  const retry = () => setRetryToken((token) => token + 1)

  return (
    <div className={styles.page}>
      <header className={styles.header}>
        <h1 className={styles.title}>Interview notes</h1>
        <Button variant="primary" onClick={onCreateNew}>
          New interview note
        </Button>
      </header>

      {loadState === 'loading' && (
        <p className={styles.status} role="status">
          Loading your interview notes…
        </p>
      )}

      {loadState === 'error' && (
        <>
          <p role="alert">{error}</p>
          <Button onClick={retry}>Try again</Button>
        </>
      )}

      {loadState === 'ready' &&
        (notes.length === 0 ? (
          <EmptyState
            title="No interview notes yet"
            description="Record what happened in a real interview — questions asked, gaps observed, and lessons learned."
            action={{ label: 'New interview note', onClick: onCreateNew }}
          />
        ) : (
          <ul className={styles.list}>
            {notes.map((note) => (
              <li key={note.id} className={styles.row}>
                <button type="button" className={styles.link} onClick={() => onSelectNote(note.id)}>
                  <span className={styles.main}>
                    <span className={styles.title2}>
                      {note.company} — {note.role}
                    </span>
                    <Chip>
                      {note.interviewRound === 'Other' ? note.otherLabel : note.interviewRound} · #{toNumber(note.sequenceNumber)}
                    </Chip>
                  </span>
                  <span className={styles.updated}>{formatDate(note.date)}</span>
                  <Icon name="chevron-right" className={styles.chevron} />
                </button>
              </li>
            ))}
          </ul>
        ))}
    </div>
  )
}
