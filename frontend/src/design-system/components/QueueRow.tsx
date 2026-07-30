import { Icon } from '../Icon'
import styles from './QueueRow.module.css'

export type QueueRowItem = {
  id: string
  title: string
  category: string
  effectiveScore: number | string
  priorityOverrideReason: string | null
  lastReviewedAtUtc: string | null
}

type QueueRowProps = {
  item: QueueRowItem
  onSelect: (id: string) => void
}

// One StudyItem in the ranked queue (components.md "QueueRow") — a row, not a card. Renders only
// values the ranked-queue API already computed; the "reason" text is either the API-provided
// PriorityOverrideReason or a plain review-recency phrase, never a recomputed score.
//
// components.md asks for a semantic link when the row navigates, but this app has no client-side
// router yet (Phase 0 kept navigation to a hand-rolled useState switch) — a real <a href> would
// need a route that doesn't exist, so this is a button acting as the row's single click target.
export function QueueRow({ item, onSelect }: QueueRowProps) {
  const reason = item.priorityOverrideReason ?? (item.lastReviewedAtUtc ? `Last reviewed ${formatDate(item.lastReviewedAtUtc)}` : 'Not yet reviewed')

  return (
    <li className={styles.row}>
      <button type="button" className={styles.link} onClick={() => onSelect(item.id)}>
        <span className={styles.main}>
          <span className={styles.title}>{item.title}</span>
          <span className={styles.category}>{item.category}</span>
        </span>
        <span className={styles.reason}>{reason}</span>
        <span className={styles.score}>{item.effectiveScore}</span>
        <Icon name="chevron-right" className={styles.chevron} />
      </button>
    </li>
  )
}

function formatDate(isoDate: string): string {
  return new Date(isoDate).toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric' })
}
