import { useState, type ReactNode } from 'react'
import { Button } from '../../design-system/components/Button'
import { EntryListEditor } from '../../design-system/components/EntryListEditor'
import layout from './FormLayout.module.css'

type CollectionSectionProps<T> = {
  entries: T[]
  onSaved: (entries: T[]) => void
  createEntry: () => T
  getKey: (entry: T) => string
  addLabel: string
  emptyLabel: string
  renderEntry: (entry: T, onChange: (next: T) => void, onRemove: () => void) => ReactNode
  save: (entries: T[]) => Promise<void>
}

// One PUT-whole-collection endpoint per ProfessionalProfile child collection (no per-entry
// create/delete routes) means each section edits and saves independently — this wraps
// EntryListEditor with that save/error/isSaving lifecycle so the seven call sites in
// ProfessionalProfilePage don't each repeat it. `draft` intentionally seeds once from `entries`
// and never resyncs from the prop — the page only mounts one section's panel at a time (via
// Tabs), so switching away and back is what gives a section its fresh starting point.
export function CollectionSection<T>({ entries, onSaved, createEntry, getKey, addLabel, emptyLabel, renderEntry, save }: CollectionSectionProps<T>) {
  const [draft, setDraft] = useState(entries)
  const [error, setError] = useState<string | null>(null)
  const [isSaving, setIsSaving] = useState(false)

  const handleSave = async () => {
    setIsSaving(true)
    setError(null)
    try {
      await save(draft)
      onSaved(draft)
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : 'Something went wrong saving this section.')
    } finally {
      setIsSaving(false)
    }
  }

  return (
    <div className={layout.stack}>
      <EntryListEditor entries={draft} onChange={setDraft} createEntry={createEntry} getKey={getKey} addLabel={addLabel} emptyLabel={emptyLabel} renderEntry={renderEntry} />
      {error && <p role="alert">{error}</p>}
      <Button type="button" variant="primary" onClick={handleSave} isLoading={isSaving}>
        Save
      </Button>
    </div>
  )
}
