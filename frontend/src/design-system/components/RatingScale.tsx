import { useRef, type KeyboardEvent } from 'react'
import styles from './RatingScale.module.css'

const RATINGS = [1, 2, 3, 4, 5]

type RatingScaleProps = {
  label: string
  value: number
  onChange: (value: number) => void
}

// The shared 1-5 control for Importance, InitialMastery and StudyReview confidence
// (components.md "RatingScale") — a real radiogroup with roving tabindex and arrow-key
// navigation, not a row of independent buttons.
export function RatingScale({ label, value, onChange }: RatingScaleProps) {
  const optionRefs = useRef<Array<HTMLButtonElement | null>>([])

  const select = (rating: number) => {
    onChange(rating)
    optionRefs.current[rating - 1]?.focus()
  }

  const handleKeyDown = (event: KeyboardEvent<HTMLDivElement>) => {
    if (event.key === 'ArrowRight' || event.key === 'ArrowUp') {
      event.preventDefault()
      select(Math.min(5, value + 1))
    } else if (event.key === 'ArrowLeft' || event.key === 'ArrowDown') {
      event.preventDefault()
      select(Math.max(1, value - 1))
    }
  }

  return (
    <div className={styles.group} role="radiogroup" aria-label={label} onKeyDown={handleKeyDown}>
      {RATINGS.map((rating) => {
        const checked = value === rating
        return (
          <button
            key={rating}
            ref={(element) => {
              optionRefs.current[rating - 1] = element
            }}
            type="button"
            role="radio"
            aria-checked={checked}
            tabIndex={checked ? 0 : -1}
            className={styles.option}
            onClick={() => select(rating)}
          >
            {rating}
          </button>
        )
      })}
    </div>
  )
}
