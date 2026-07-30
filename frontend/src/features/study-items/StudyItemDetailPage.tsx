import { useCallback, useEffect, useState } from 'react'
import { Button } from '../../design-system/components/Button'
import { Chip } from '../../design-system/components/Chip'
import { Icon } from '../../design-system/Icon'
import { ScoreBreakdown } from '../../design-system/components/ScoreBreakdown'
import {
  archiveStudyItem,
  clearPriorityOverride,
  deleteStudyItem,
  fetchStudyItem,
  setPriorityOverride,
  submitStudyReview,
  toNumber,
  type StudyItemResponse,
} from './api'
import { DetailsSummary } from './details/DetailsSummary'
import { EditStudyItemForm } from './EditStudyItemForm'
import { PriorityOverrideForm } from './PriorityOverrideForm'
import { ReviewForm } from './ReviewForm'
import styles from './StudyItemDetailPage.module.css'

type LoadState = 'loading' | 'ready' | 'not-found' | 'error'

type StudyItemDetailPageProps = {
  itemId: string
  onBack: () => void
  onDeleted: () => void
}

function describeError(caught: unknown, fallback: string): string {
  return caught instanceof Error ? caught.message : fallback
}

export function StudyItemDetailPage({ itemId, onBack, onDeleted }: StudyItemDetailPageProps) {
  const [loadState, setLoadState] = useState<LoadState>('loading')
  const [item, setItem] = useState<StudyItemResponse | null>(null)
  const [loadError, setLoadError] = useState<string | null>(null)
  const [isEditing, setIsEditing] = useState(false)
  const [confirmingDelete, setConfirmingDelete] = useState(false)
  const [actionError, setActionError] = useState<string | null>(null)
  const [isBusy, setIsBusy] = useState(false)

  // No explicit setLoadState('loading') here: the caller renders this component with
  // key={itemId} (see App.tsx), so a new itemId is a fresh mount and loadState's initial value
  // already covers it. "Try again" (an event handler, not this effect) resets it explicitly.
  const load = useCallback(async () => {
    try {
      const data = await fetchStudyItem(itemId)
      if (!data) {
        setLoadState('not-found')
        return
      }

      setItem(data)
      setLoadState('ready')
    } catch (caught) {
      setLoadError(describeError(caught, 'Something went wrong loading this study item.'))
      setLoadState('error')
    }
  }, [itemId])

  // Inlined rather than calling load() directly — the linter's set-state-in-effect rule treats
  // any call to a function reference that sets state as synchronous, regardless of the await
  // inside it (see StudyQueuePage for the same pattern).
  useEffect(() => {
    fetchStudyItem(itemId)
      .then((data) => {
        if (!data) {
          setLoadState('not-found')
          return
        }

        setItem(data)
        setLoadState('ready')
      })
      .catch((caught: unknown) => {
        setLoadError(describeError(caught, 'Something went wrong loading this study item.'))
        setLoadState('error')
      })
  }, [itemId])

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
        <p>This study item could not be found.</p>
        <Button onClick={onBack}>Back to queue</Button>
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

  const data = item!

  if (isEditing) {
    return (
      <EditStudyItemForm
        item={data}
        onSaved={() => {
          setIsEditing(false)
          void load()
        }}
        onCancel={() => setIsEditing(false)}
      />
    )
  }

  const canHardDelete = data.reviews.length === 0

  const handleArchive = async () => {
    setIsBusy(true)
    setActionError(null)
    try {
      await archiveStudyItem(data.id)
      await load()
    } catch (caught) {
      setActionError(describeError(caught, 'Something went wrong archiving this study item.'))
    } finally {
      setIsBusy(false)
    }
  }

  const handleDelete = async () => {
    setIsBusy(true)
    setActionError(null)
    try {
      await deleteStudyItem(data.id)
      onDeleted()
    } catch (caught) {
      setActionError(describeError(caught, 'Something went wrong deleting this study item.'))
      setIsBusy(false)
      setConfirmingDelete(false)
    }
  }

  const handleReview = async (confidenceRating: number, notesMarkdown: string | null) => {
    await submitStudyReview(data.id, confidenceRating, notesMarkdown)
    await load()
  }

  const handleSetOverride = async (score: number, reason: string) => {
    await setPriorityOverride(data.id, score, reason)
    await load()
  }

  const handleClearOverride = async () => {
    setIsBusy(true)
    setActionError(null)
    try {
      await clearPriorityOverride(data.id)
      await load()
    } catch (caught) {
      setActionError(describeError(caught, 'Something went wrong clearing the priority override.'))
    } finally {
      setIsBusy(false)
    }
  }

  return (
    <div className={styles.page}>
      <Button variant="ghost" className={styles.back} onClick={onBack}>
        Back to queue
      </Button>

      <header className={styles.header}>
        <div className={styles.titleGroup}>
          <h1 className={styles.title}>{data.title}</h1>
          <p className={styles.meta}>
            <span>{data.category}</span>
            <span>·</span>
            <span>{data.status}</span>
          </p>
        </div>
        <div className={styles.actions}>
          <Button variant="secondary" onClick={() => setIsEditing(true)}>
            <Icon name="pencil" /> Edit
          </Button>
          {data.status === 'Active' && (
            <Button variant="secondary" onClick={handleArchive} disabled={isBusy}>
              Archive
            </Button>
          )}
          {canHardDelete &&
            (confirmingDelete ? (
              <span className={styles.confirmRow}>
                <span>Delete this study item permanently?</span>
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
            ))}
        </div>
      </header>

      {!canHardDelete && (
        <p className={styles.guard}>
          This study item has {data.reviews.length} review{data.reviews.length === 1 ? '' : 's'} and can&apos;t be deleted. Archive it instead to remove it from the
          queue.
        </p>
      )}

      {actionError && <p role="alert">{actionError}</p>}

      <section className={styles.section} aria-label="Score">
        <ScoreBreakdown
          effectiveScore={toNumber(data.effectiveScore)}
          importanceContribution={toNumber(data.scoreBreakdown.importanceContribution)}
          demandContribution={toNumber(data.scoreBreakdown.demandContribution)}
          masteryGapContribution={toNumber(data.scoreBreakdown.masteryGapContribution)}
        />
      </section>

      <section className={styles.section} aria-label="Priority override">
        <h2 className={styles.sectionTitle}>Priority override</h2>
        {data.priorityOverrideScore != null ? (
          <div className={styles.overrideRow}>
            <span>
              Score {data.priorityOverrideScore} — {data.priorityOverrideReason}
            </span>
            <Button variant="ghost" onClick={handleClearOverride} disabled={isBusy}>
              Clear
            </Button>
          </div>
        ) : (
          <PriorityOverrideForm onSubmit={handleSetOverride} />
        )}
      </section>

      <section className={styles.section} aria-label="Tags">
        <h2 className={styles.sectionTitle}>Tags</h2>
        <div className={styles.tags}>
          {data.tags.length === 0 ? (
            <span className={styles.status}>No tags.</span>
          ) : (
            data.tags.map((tag) => <Chip key={tag}>{tag}</Chip>)
          )}
        </div>
      </section>

      <section className={styles.section} aria-label="Details">
        <h2 className={styles.sectionTitle}>Details</h2>
        <DetailsSummary details={data.details} />
      </section>

      <section className={styles.section} aria-label="Review history">
        <h2 className={styles.sectionTitle}>Reviews</h2>
        {data.reviews.length === 0 ? (
          <p className={styles.status}>No reviews yet.</p>
        ) : (
          <ul className={styles.reviewList}>
            {data.reviews.map((review) => (
              <li key={review.id} className={styles.reviewItem}>
                <span className={styles.reviewMeta}>
                  {new Date(review.reviewedAtUtc).toLocaleDateString()} · confidence {review.confidenceRating}
                </span>
                {review.notesMarkdown && <p className={styles.reviewNotes}>{review.notesMarkdown}</p>}
              </li>
            ))}
          </ul>
        )}
        <ReviewForm onSubmit={handleReview} />
      </section>
    </div>
  )
}
