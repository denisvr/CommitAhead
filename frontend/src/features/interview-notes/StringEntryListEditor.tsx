import { useState } from 'react'
import { Button } from '../../design-system/components/Button'
import { EntryListEditor } from '../../design-system/components/EntryListEditor'
import inputStyles from '../../design-system/components/Input.module.css'
import styles from './StringEntryListEditor.module.css'

type StringEntry = { id: string; value: string }

type StringEntryListEditorProps = {
  label: string
  values: string[]
  onChange: (values: string[]) => void
  addLabel: string
  emptyLabel: string
}

// EntryListEditor expects entries with a stable id (see its own test file) — plain strings have
// none, so this wraps each one in a locally-generated { id, value } pair for editor state only.
// Seeded once from `values` on mount, the same one-time-seed convention CVPresentationForm/
// InterviewNoteForm already use for their own initial state — this component is only ever
// mounted once per form lifetime, never resynced from a changing prop.
export function StringEntryListEditor({ label, values, onChange, addLabel, emptyLabel }: StringEntryListEditorProps) {
  const [entries, setEntries] = useState<StringEntry[]>(() => values.map((value) => ({ id: crypto.randomUUID(), value })))

  const handleChange = (next: StringEntry[]) => {
    setEntries(next)
    onChange(next.map((entry) => entry.value))
  }

  return (
    <div className={styles.field}>
      <span className={styles.label}>{label}</span>
      <EntryListEditor<StringEntry>
        entries={entries}
        onChange={handleChange}
        createEntry={() => ({ id: crypto.randomUUID(), value: '' })}
        getKey={(entry) => entry.id}
        addLabel={addLabel}
        emptyLabel={emptyLabel}
        renderEntry={(entry, onEntryChange, onRemove) => {
          const index = entries.findIndex((candidate) => candidate.id === entry.id)
          return (
            <div className={styles.row}>
              <input
                aria-label={`${label} entry ${index + 1}`}
                type="text"
                className={inputStyles.input}
                value={entry.value}
                onChange={(event) => onEntryChange({ ...entry, value: event.target.value })}
              />
              <Button type="button" variant="ghost" onClick={onRemove} aria-label={`Remove ${label.toLowerCase()} entry ${index + 1}`}>
                Remove
              </Button>
            </div>
          )
        }}
      />
    </div>
  )
}
