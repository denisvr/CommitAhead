import type { ReactNode } from 'react'
import { Icon } from '../Icon'
import styles from './Chip.module.css'

type ChipProps = {
  children: ReactNode
  onClick?: () => void
  onRemove?: () => void
  removeLabel?: string
}

// A compact tag/filter control (components.md "Chip") — monochrome, never a generic container.
// `onClick` makes the chip itself an interactive trigger; the remove button stays a real <button>
// nested inside, so the chip is a <span role="button"> rather than a <button> to avoid nesting
// one button inside another.
export function Chip({ children, onClick, onRemove, removeLabel }: ChipProps) {
  return (
    <span
      className={styles.chip}
      role={onClick ? 'button' : undefined}
      tabIndex={onClick ? 0 : undefined}
      onClick={onClick}
      onKeyDown={
        onClick
          ? (event) => {
              if (event.key === 'Enter' || event.key === ' ') {
                event.preventDefault()
                onClick()
              }
            }
          : undefined
      }
    >
      {children}
      {onRemove && (
        <button
          type="button"
          className={styles.remove}
          onClick={(event) => {
            event.stopPropagation()
            onRemove()
          }}
          aria-label={removeLabel}
        >
          <Icon name="x" />
        </button>
      )}
    </span>
  )
}
