import type { ReactNode } from 'react'
import { Icon } from '../Icon'
import styles from './Chip.module.css'

type ChipProps = {
  children: ReactNode
  onClick?: () => void
  onRemove?: () => void
  removeLabel?: string
  disabled?: boolean
}

// A compact tag/filter control (components.md "Chip") — monochrome, never a generic container.
// `onClick` makes the chip itself an interactive trigger; the remove button stays a real <button>
// nested inside, so the chip is a <span role="button"> rather than a <button> to avoid nesting
// one button inside another.
export function Chip({ children, onClick, onRemove, removeLabel, disabled = false }: ChipProps) {
  const handleClick = disabled ? undefined : onClick
  return (
    <span
      className={styles.chip}
      role={handleClick ? 'button' : undefined}
      tabIndex={handleClick ? 0 : undefined}
      aria-disabled={disabled || undefined}
      onClick={handleClick}
      onKeyDown={
        handleClick
          ? (event) => {
              if (event.key === 'Enter' || event.key === ' ') {
                event.preventDefault()
                handleClick()
              }
            }
          : undefined
      }
    >
      {children}
      {onRemove && (
        <button
          type="button"
          disabled={disabled}
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
