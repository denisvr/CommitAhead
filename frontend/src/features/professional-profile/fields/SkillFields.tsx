import { Icon } from '../../../design-system/Icon'
import type { SkillCategory, SkillDto, SkillProficiency } from '../api'
import styles from '../sections/sections.module.css'

type SkillFieldsProps = {
  value: SkillDto
  isEditing: boolean
  disabled?: boolean
  onChange: (value: SkillDto) => void
  onRemove: () => void
  onStartEdit: () => void
  onStopEdit: () => void
}

const CATEGORIES: SkillCategory[] = [
  'Language',
  'Framework',
  'Platform',
  'Cloud',
  'Database',
  'Messaging',
  'DevOps',
  'Testing',
  'Architecture',
  'Tool',
  'Methodology',
  'Domain',
  'Other',
]

const PROFICIENCIES: SkillProficiency[] = ['Beginner', 'Intermediate', 'Advanced', 'Expert']

// One <tr> in the enclosing ".sk" table (SkillsSection). Read-only text by default (Europass-
// style) — Edit swaps the row into its input/select cells, Done swaps it back. The reference's
// Skill/Level/Years/Last used columns become Skill/Category/Proficiency here — Years and Last
// used aren't fields on SkillDto, and CLAUDE.md is explicit that later-phase behaviour is never
// built from a mock, so they're dropped rather than invented for a pixel-exact column count.
export function SkillFields({ value, isEditing, disabled = false, onChange, onRemove, onStartEdit, onStopEdit }: SkillFieldsProps) {
  if (!isEditing) {
    return (
      <tr>
        <td>{value.displayName || 'Untitled skill'}</td>
        <td>{value.category}</td>
        <td>{value.proficiency ?? '—'}</td>
        <td className={styles.skillActionCell}>
          <span className={styles.skillActions}>
            <button type="button" disabled={disabled} className={`${styles.iconRemove} ${styles.iconEdit}`} onClick={onStartEdit} aria-label={`Edit ${value.displayName || 'skill'}`}>
              <Icon name="pencil" />
            </button>
            <button type="button" disabled={disabled} className={`${styles.iconRemove} ${styles.iconDelete}`} onClick={onRemove} aria-label={`Remove ${value.displayName || 'skill'}`}>
              <Icon name="x" />
            </button>
          </span>
        </td>
      </tr>
    )
  }

  return (
    <tr>
      <td>
        <input type="text" required aria-label="Skill" value={value.displayName} onChange={(event) => onChange({ ...value, displayName: event.target.value })} />
      </td>
      <td>
        <select aria-label="Category" value={value.category} onChange={(event) => onChange({ ...value, category: event.target.value as SkillCategory })}>
          {CATEGORIES.map((option) => (
            <option key={option} value={option}>
              {option}
            </option>
          ))}
        </select>
      </td>
      <td>
        <select
          aria-label="Proficiency"
          value={value.proficiency ?? ''}
          onChange={(event) => onChange({ ...value, proficiency: (event.target.value || null) as SkillProficiency })}
        >
          <option value="">Unspecified</option>
          {PROFICIENCIES.map((option) => (
            <option key={option} value={option ?? ''}>
              {option}
            </option>
          ))}
        </select>
      </td>
      <td className={styles.skillActionCell}>
        <span className={styles.skillActions}>
          <button type="button" disabled={disabled} className={`${styles.iconRemove} ${styles.iconDone}`} onClick={onStopEdit} aria-label={`Done editing ${value.displayName || 'skill'}`}>
            <Icon name="check" />
          </button>
          <button type="button" disabled={disabled} className={`${styles.iconRemove} ${styles.iconDelete}`} onClick={onRemove} aria-label={`Remove ${value.displayName || 'skill'}`}>
            <Icon name="x" />
          </button>
        </span>
      </td>
    </tr>
  )
}
