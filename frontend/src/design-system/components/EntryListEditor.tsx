import type { ReactNode } from 'react'
import { Button } from './Button'
import styles from './EntryListEditor.module.css'

type EntryListEditorProps<T> = {
  entries: T[]
  onChange: (entries: T[]) => void
  createEntry: () => T
  getKey: (entry: T) => string
  addLabel: string
  emptyLabel: string
  renderEntry: (entry: T, onChange: (next: T) => void, onRemove: () => void) => ReactNode
}

// Controlled the same way TagInput is (value + onChange, no internal copy of the list) — the
// structured-entry counterpart for ProfessionalProfile's seven whole-collection-replace
// endpoints, none of which have per-entry create/delete routes. This component owns only
// add/remove/update-in-place array mechanics; the actual fields for each entry type are supplied
// by the caller via renderEntry, mirroring Field's render-prop convention.
export function EntryListEditor<T>({ entries, onChange, createEntry, getKey, addLabel, emptyLabel, renderEntry }: EntryListEditorProps<T>) {
  const addEntry = () => onChange([...entries, createEntry()])

  const updateEntry = (key: string, next: T) => onChange(entries.map((entry) => (getKey(entry) === key ? next : entry)))

  const removeEntry = (key: string) => onChange(entries.filter((entry) => getKey(entry) !== key))

  return (
    <div className={styles.list}>
      {entries.length === 0 && <p className={styles.empty}>{emptyLabel}</p>}
      {entries.map((entry) => {
        const key = getKey(entry)
        return (
          <div key={key} className={styles.entry}>
            {renderEntry(entry, (next) => updateEntry(key, next), () => removeEntry(key))}
          </div>
        )
      })}
      <Button type="button" variant="secondary" onClick={addEntry}>
        {addLabel}
      </Button>
    </div>
  )
}
