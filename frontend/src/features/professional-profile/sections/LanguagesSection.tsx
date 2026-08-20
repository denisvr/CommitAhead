import { useState } from 'react'
import { Button } from '../../../design-system/components/Button'
import { Card } from '../../../design-system/components/Card'
import { EmptyState } from '../../../design-system/components/EmptyState'
import { replaceLanguages, type LanguageEntryDto } from '../api'
import { LanguageEntryFields } from '../fields/LanguageEntryFields'
import { useSectionSave } from '../useEditableCollection'
import styles from './sections.module.css'

type LanguagesSectionProps = {
  languages: LanguageEntryDto[]
  onChange: (languages: LanguageEntryDto[]) => void
}

const createEntry = (): LanguageEntryDto => ({ id: crypto.randomUUID(), language: '', proficiency: 'B1', certification: null })

// The approved layout shows a four-skill CEFR breakdown (listening/reading/speaking/writing) per
// language; LanguageEntry only stores one overall proficiency. Showing four columns here would
// mean inventing three values nobody entered, so this renders the one real attribute instead.
export function LanguagesSection({ languages, onChange }: LanguagesSectionProps) {
  const { error, isSaving, handleSave } = useSectionSave(languages, replaceLanguages)
  const [editingIds, setEditingIds] = useState<Set<string>>(new Set())

  const setEditing = (id: string, value: boolean) => {
    const next = new Set(editingIds)
    if (value) next.add(id)
    else next.delete(id)
    setEditingIds(next)
  }

  const addEntry = () => {
    const entry = createEntry()
    onChange([...languages, entry])
    setEditing(entry.id, true)
  }

  // No separate Save button — Done (the check icon) persists the whole current list immediately,
  // since the API only offers whole-collection replace; the "per item" feel comes from the UI
  // action, not a per-entity endpoint. Edit mode only closes once the save actually succeeds, so a
  // failed save leaves the entry open (with the error shown) rather than looking saved.
  const stopEditingAndSave = async (id: string) => {
    const success = await handleSave()
    if (success) setEditing(id, false)
  }

  const removeEntry = async (id: string) => {
    const previous = languages
    const next = languages.filter((item) => item.id !== id)
    onChange(next)
    const success = await handleSave(next)
    if (!success) onChange(previous)
  }

  return (
    <Card
      id="languages"
      icon="languages"
      heading="Languages"
      meta={`${languages.length} language${languages.length === 1 ? '' : 's'}`}
      actions={
        <Button type="button" variant="success" size="sm" onClick={addEntry} disabled={isSaving}>
          + Add a language
        </Button>
      }
    >
      {languages.length === 0 ? (
        <EmptyState title="No languages yet" description="Add every language you'd list on a CV." />
      ) : (
        <>
          <div className={`${styles.langRow} ${styles.langHeader}`}>
            <span>Language</span>
            <span>Proficiency</span>
            <span>Certification</span>
            <span />
          </div>
          {languages.map((entry) => (
            <LanguageEntryFields
              key={entry.id}
              value={entry}
              isEditing={editingIds.has(entry.id)}
              disabled={isSaving}
              onChange={(next) => onChange(languages.map((item) => (item.id === entry.id ? next : item)))}
              onRemove={() => void removeEntry(entry.id)}
              onStartEdit={() => setEditing(entry.id, true)}
              onStopEdit={() => void stopEditingAndSave(entry.id)}
            />
          ))}
        </>
      )}

      {error && (
        <p role="alert" className={styles.error}>
          {error}
        </p>
      )}
    </Card>
  )
}
