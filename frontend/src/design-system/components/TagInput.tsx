import { useId, useState, type KeyboardEvent } from 'react'
import { Chip } from './Chip'
import styles from './TagInput.module.css'

type TagInputProps = {
  label: string
  value: string[]
  onChange: (tags: string[]) => void
}

// Normalisation (lowercase, kebab-case, dedup) is a domain rule (TagNormalizer) applied
// server-side — this control only collects the raw strings the user typed.
export function TagInput({ label, value, onChange }: TagInputProps) {
  const [draft, setDraft] = useState('')
  const id = useId()

  const addDraftTag = () => {
    const trimmed = draft.trim()
    if (trimmed) {
      onChange([...value, trimmed])
    }
    setDraft('')
  }

  const removeTag = (tag: string) => {
    onChange(value.filter((existing) => existing !== tag))
  }

  const handleKeyDown = (event: KeyboardEvent<HTMLInputElement>) => {
    if (event.key === 'Enter' || event.key === ',') {
      event.preventDefault()
      addDraftTag()
    } else if (event.key === 'Backspace' && draft === '' && value.length > 0) {
      removeTag(value[value.length - 1])
    }
  }

  return (
    <div className={styles.wrapper}>
      <label htmlFor={id} className={styles.label}>
        {label}
      </label>
      <div className={styles.field}>
        {value.map((tag) => (
          <Chip key={tag} onRemove={() => removeTag(tag)} removeLabel={`Remove tag ${tag}`}>
            {tag}
          </Chip>
        ))}
        <input
          id={id}
          type="text"
          className={styles.input}
          value={draft}
          onChange={(event) => setDraft(event.target.value)}
          onKeyDown={handleKeyDown}
          onBlur={addDraftTag}
          placeholder="Add a tag"
        />
      </div>
    </div>
  )
}
