import { Icon } from '../../../design-system/Icon'
import type { LanguageEntryDto, LanguageProficiency } from '../api'
import styles from '../sections/sections.module.css'

type LanguageEntryFieldsProps = {
  value: LanguageEntryDto
  isEditing: boolean
  disabled?: boolean
  onChange: (value: LanguageEntryDto) => void
  onRemove: () => void
  onStartEdit: () => void
  onStopEdit: () => void
}

const PROFICIENCIES: LanguageProficiency[] = ['A1', 'A2', 'B1', 'B2', 'C1', 'C2', 'Native']

// One ".lg" row. Read-only text by default (Europass-style) — Edit swaps the row into its
// input/select cells, Done swaps it back. The reference shows a four-skill CEFR breakdown
// (listening/reading/speaking/writing) per language; LanguageEntry only stores one overall
// proficiency, so this renders the one real attribute instead of inventing three values nobody
// entered. The "Native" badge is derived from that same field, not a separate invented flag.
export function LanguageEntryFields({ value, isEditing, disabled = false, onChange, onRemove, onStartEdit, onStopEdit }: LanguageEntryFieldsProps) {
  if (!isEditing) {
    return (
      <div className={styles.langRow}>
        <span className={styles.langName}>
          <span className={styles.langValue}>{value.language || 'Untitled language'}</span>
          {value.proficiency === 'Native' && <span className={styles.native}>Native</span>}
        </span>
        <span className={styles.langValue}>{value.proficiency}</span>
        <span className={styles.langValue}>{value.certification || '—'}</span>
        <span className={styles.skillActions}>
          <button type="button" disabled={disabled} className={`${styles.iconRemove} ${styles.iconEdit}`} onClick={onStartEdit} aria-label={`Edit ${value.language || 'language'}`}>
            <Icon name="pencil" />
          </button>
          <button type="button" disabled={disabled} className={`${styles.iconRemove} ${styles.iconDelete}`} onClick={onRemove} aria-label={`Remove ${value.language || 'language'}`}>
            <Icon name="x" />
          </button>
        </span>
      </div>
    )
  }

  return (
    <div className={styles.langRow}>
      <span className={styles.langName}>
        <input type="text" required aria-label="Language" value={value.language} onChange={(event) => onChange({ ...value, language: event.target.value })} />
        {value.proficiency === 'Native' && <span className={styles.native}>Native</span>}
      </span>
      <select aria-label="Proficiency" value={value.proficiency} onChange={(event) => onChange({ ...value, proficiency: event.target.value as LanguageProficiency })}>
        {PROFICIENCIES.map((option) => (
          <option key={option} value={option}>
            {option}
          </option>
        ))}
      </select>
      <input
        type="text"
        aria-label="Certification"
        placeholder="Certification"
        value={value.certification ?? ''}
        onChange={(event) => onChange({ ...value, certification: event.target.value || null })}
      />
      <span className={styles.skillActions}>
        <button type="button" disabled={disabled} className={`${styles.iconRemove} ${styles.iconDone}`} onClick={onStopEdit} aria-label={`Done editing ${value.language || 'language'}`}>
          <Icon name="check" />
        </button>
        <button type="button" disabled={disabled} className={`${styles.iconRemove} ${styles.iconDelete}`} onClick={onRemove} aria-label={`Remove ${value.language || 'language'}`}>
          <Icon name="x" />
        </button>
      </span>
    </div>
  )
}
