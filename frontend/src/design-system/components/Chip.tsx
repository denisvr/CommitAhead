import type { ReactNode } from 'react'
import { Icon } from '../Icon'
import styles from './Chip.module.css'

type ChipProps = {
  children: ReactNode
  onRemove?: () => void
  removeLabel?: string
}

// A compact tag/filter control (components.md "Chip") — monochrome, never a generic container.
export function Chip({ children, onRemove, removeLabel }: ChipProps) {
  return (
    <span className={styles.chip}>
      {children}
      {onRemove && (
        <button type="button" className={styles.remove} onClick={onRemove} aria-label={removeLabel}>
          <Icon name="x" />
        </button>
      )}
    </span>
  )
}
