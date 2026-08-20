import { useId, useState, type KeyboardEvent } from 'react'
import { Icon } from '../Icon'
import { Badge } from './Badge'
import inputStyles from './Input.module.css'
import styles from './AchievementRepository.module.css'

type AchievementRepositoryProps = {
  achievements: string[]
  onChange: (achievements: string[]) => void
  onHighlight?: (index: number | null) => void
}

// Profile = truth, CV = strategy: this list is deliberately unbounded. A CV later selects three
// or four of these for a given role; nothing here should imply a target count.
export function AchievementRepository({ achievements, onChange, onHighlight }: AchievementRepositoryProps) {
  const [draft, setDraft] = useState('')
  const id = useId()

  const updateAt = (index: number, next: string) => onChange(achievements.map((achievement, i) => (i === index ? next : achievement)))

  const removeAt = (index: number) => onChange(achievements.filter((_, i) => i !== index))

  const commitDraft = () => {
    const trimmed = draft.trim()
    if (trimmed) onChange([...achievements, trimmed])
    setDraft('')
  }

  const handleDraftKeyDown = (event: KeyboardEvent<HTMLInputElement>) => {
    if (event.key === 'Enter') {
      event.preventDefault()
      commitDraft()
    }
  }

  return (
    <div className={styles.repo}>
      <div className={styles.head}>
        <h4 className={styles.title}>Impact and achievements</h4>
        <span className={styles.spacer} />
        {achievements.length === 0 ? <Badge tone="caution">0 captured</Badge> : <span className={styles.count}>{achievements.length} captured</span>}
      </div>
      <p className={styles.lead}>
        Add everything worth remembering — there is no right number here. A CV typically prints three or four of these, chosen for the role it targets.
      </p>

      <ul className={styles.list}>
        {achievements.map((achievement, index) => (
          <li key={index} className={styles.row}>
            <input
              type="text"
              className={inputStyles.input}
              value={achievement}
              onChange={(event) => updateAt(index, event.target.value)}
              onFocus={() => onHighlight?.(index)}
              onBlur={() => onHighlight?.(null)}
              onMouseEnter={() => onHighlight?.(index)}
              onMouseLeave={() => onHighlight?.(null)}
            />
            <button type="button" className={styles.remove} onClick={() => removeAt(index)} aria-label={`Remove achievement ${index + 1}`}>
              <Icon name="x" />
            </button>
          </li>
        ))}
      </ul>

      <div className={styles.addRow}>
        <label htmlFor={id} className={styles.addLabel}>
          Add another
        </label>
        <input
          id={id}
          type="text"
          className={inputStyles.input}
          placeholder="Add another — even if no CV would ever print it"
          value={draft}
          onChange={(event) => setDraft(event.target.value)}
          onKeyDown={handleDraftKeyDown}
          onBlur={commitDraft}
        />
      </div>
    </div>
  )
}
