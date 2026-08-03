import { Chip } from './Chip'
import { Icon } from '../Icon'
import styles from './StudyItemRow.module.css'

export type StudyItemRowItem = {
  id: string
  title: string
  category: string
  status: string
  updatedAtUtc: string
}

type StudyItemRowProps = {
  item: StudyItemRowItem
  onSelect: (id: string) => void
}

// One StudyItem in the plain Study Items list (Active + Archived) — the sibling of QueueRow for
// the non-ranked list view (components.md "AppShell" destination 2). Shows status instead of a
// score, since this list has no EffectiveScore to display.
export function StudyItemRow({ item, onSelect }: StudyItemRowProps) {
  return (
    <li className={styles.row}>
      <button type="button" className={styles.link} onClick={() => onSelect(item.id)}>
        <span className={styles.main}>
          <span className={styles.title}>{item.title}</span>
          <span className={styles.category}>{item.category}</span>
        </span>
        <Chip>{item.status}</Chip>
        <span className={styles.updated}>Updated {formatDate(item.updatedAtUtc)}</span>
        <Icon name="chevron-right" className={styles.chevron} />
      </button>
    </li>
  )
}

function formatDate(isoDate: string): string {
  return new Date(isoDate).toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric' })
}
