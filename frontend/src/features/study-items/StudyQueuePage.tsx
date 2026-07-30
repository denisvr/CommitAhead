import { useCallback, useEffect, useState } from 'react'
import { Button } from '../../design-system/components/Button'
import { EmptyState } from '../../design-system/components/EmptyState'
import { QueueRow } from '../../design-system/components/QueueRow'
import { fetchStudyQueue, type RankedStudyItemResponse } from './api'
import styles from './StudyQueuePage.module.css'

type LoadState = 'loading' | 'ready' | 'error'

type StudyQueuePageProps = {
  onSelectItem: (id: string) => void
  onCreateNew: () => void
}

// page-patterns.md "Study queue": lead with the single next item, follow with the remaining
// ordered rows, all values and ordering taken directly from the ranked-queue API projection.
export function StudyQueuePage({ onSelectItem, onCreateNew }: StudyQueuePageProps) {
  const [loadState, setLoadState] = useState<LoadState>('loading')
  const [items, setItems] = useState<RankedStudyItemResponse[]>([])
  const [error, setError] = useState<string | null>(null)

  const load = useCallback(async () => {
    try {
      setItems(await fetchStudyQueue())
      setLoadState('ready')
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : 'Something went wrong loading the study queue.')
      setLoadState('error')
    }
  }, [])

  useEffect(() => {
    fetchStudyQueue()
      .then((data) => {
        setItems(data)
        setLoadState('ready')
      })
      .catch((caught: unknown) => {
        setError(caught instanceof Error ? caught.message : 'Something went wrong loading the study queue.')
        setLoadState('error')
      })
  }, [])

  const retry = () => {
    setLoadState('loading')
    void load()
  }

  if (loadState === 'loading') {
    return (
      <p className={styles.status} role="status">
        Loading your study queue…
      </p>
    )
  }

  if (loadState === 'error') {
    return (
      <div className={styles.page}>
        <p role="alert">{error}</p>
        <Button onClick={retry}>Try again</Button>
      </div>
    )
  }

  const [lead, ...rest] = items

  return (
    <div className={styles.page}>
      <header className={styles.header}>
        <h1 className={styles.title}>Study queue</h1>
        <Button variant="primary" onClick={onCreateNew}>
          New study item
        </Button>
      </header>

      {!lead ? (
        <EmptyState
          title="No active study items yet"
          description="Add your first study item — a LeetCode problem, a system design prompt, a behavioral story, or a theory topic — to start building your queue."
          action={{ label: 'New study item', onClick: onCreateNew }}
        />
      ) : (
        <>
          <section className={styles.lead} aria-label="Next up">
            <p className={styles.leadLabel}>Next up</p>
            <div className={styles.leadTop}>
              <h2 className={styles.leadTitle}>{lead.title}</h2>
              <span className={styles.leadScore}>{lead.effectiveScore}</span>
            </div>
            <p className={styles.leadReason}>{describeReason(lead)}</p>
            <Button variant="primary" onClick={() => onSelectItem(lead.id)}>
              Open
            </Button>
          </section>

          {rest.length > 0 && (
            <ul className={styles.list}>
              {rest.map((item) => (
                <QueueRow key={item.id} item={item} onSelect={onSelectItem} />
              ))}
            </ul>
          )}
        </>
      )}
    </div>
  )
}

function describeReason(item: RankedStudyItemResponse): string {
  if (item.priorityOverrideReason) {
    return item.priorityOverrideReason
  }

  if (item.lastReviewedAtUtc) {
    return `Last reviewed ${new Date(item.lastReviewedAtUtc).toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric' })}`
  }

  return 'Not yet reviewed'
}
