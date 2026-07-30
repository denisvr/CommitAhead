import { Button } from './Button'
import styles from './EmptyState.module.css'

type EmptyStateProps = {
  title: string
  description: string
  action?: { label: string; onClick: () => void }
}

// Explains why a region is empty and offers one next action (components.md "EmptyState") — no
// decorative illustration or filler copy.
export function EmptyState({ title, description, action }: EmptyStateProps) {
  return (
    <div className={styles.empty}>
      <h2 className={styles.title}>{title}</h2>
      <p className={styles.description}>{description}</p>
      {action && (
        <Button variant="primary" onClick={action.onClick}>
          {action.label}
        </Button>
      )}
    </div>
  )
}
