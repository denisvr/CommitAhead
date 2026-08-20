import { useState } from 'react'
import { Button } from '../../../design-system/components/Button'
import { Card } from '../../../design-system/components/Card'
import { CollapsibleRow } from '../../../design-system/components/CollapsibleRow'
import { EmptyState } from '../../../design-system/components/EmptyState'
import { RestrictedMarkdown } from '../../../design-system/components/RestrictedMarkdown'
import { replaceEducation, type EducationEntryDto } from '../api'
import { EducationEntryFields } from '../fields/EducationEntryFields'
import { formatDateRange } from '../formatDate'
import { useDragReorder } from '../useDragReorder'
import { useSectionSave } from '../useEditableCollection'
import styles from './sections.module.css'

type EducationSectionProps = {
  education: EducationEntryDto[]
  onChange: (education: EducationEntryDto[]) => void
}

const createEntry = (): EducationEntryDto => ({ id: crypto.randomUUID(), institution: '', degree: '', field: null, startDate: null, endDate: null, location: null, detailsMarkdown: null })

function EducationEntryReadView({ value }: { value: EducationEntryDto }) {
  const metaBits = [value.field, value.location].filter(Boolean)
  return (
    <div className={styles.readView}>
      {metaBits.length > 0 && <p className={styles.readMetaLine}>{metaBits.join(' · ')}</p>}
      {value.detailsMarkdown && value.detailsMarkdown.trim() !== '' ? (
        <RestrictedMarkdown className={styles.readSummary}>{value.detailsMarkdown}</RestrictedMarkdown>
      ) : (
        <p className={styles.readEmpty}>No further details recorded.</p>
      )}
    </div>
  )
}

export function EducationSection({ education, onChange }: EducationSectionProps) {
  const { error, isSaving, handleSave } = useSectionSave(education, replaceEducation)
  const [openIds, setOpenIds] = useState<Set<string>>(() => new Set(education.length === 1 ? [education[0].id] : []))
  const [editingIds, setEditingIds] = useState<Set<string>>(new Set())

  const toggleOpen = (id: string) => {
    const next = new Set(openIds)
    if (next.has(id)) next.delete(id)
    else next.add(id)
    setOpenIds(next)
  }

  const setEditing = (id: string, value: boolean) => {
    const next = new Set(editingIds)
    if (value) next.add(id)
    else next.delete(id)
    setEditingIds(next)
  }

  const addEntry = () => {
    const entry = createEntry()
    onChange([entry, ...education])
    setOpenIds(new Set([...openIds, entry.id]))
    setEditing(entry.id, true)
  }

  // No separate Save button — Done (exiting edit) persists the whole current list immediately,
  // since the API only offers whole-collection replace; the "per item" feel comes from the UI
  // action, not a per-entity endpoint. Edit mode only closes once the save actually succeeds, so a
  // failed save leaves the entry open (with the error shown) rather than looking saved.
  const stopEditingAndSave = async (id: string) => {
    const success = await handleSave()
    if (success) setEditing(id, false)
  }

  const removeEntry = async (id: string) => {
    const previous = education
    const next = education.filter((item) => item.id !== id)
    onChange(next)
    const success = await handleSave(next)
    if (!success) onChange(previous)
  }

  // Applies the reorder optimistically but reverts to the pre-reorder array if the PUT fails — an
  // optimistic update must never linger as if it had been saved.
  const moveTo = async (id: string, delta: number) => {
    const index = education.findIndex((entry) => entry.id === id)
    const target = index + delta
    if (index < 0 || target < 0 || target >= education.length) return
    const previous = education
    const next = [...education]
    ;[next[index], next[target]] = [next[target], next[index]]
    onChange(next)
    const success = await handleSave(next)
    if (!success) onChange(previous)
  }

  const persistReorder = async (next: EducationEntryDto[]) => {
    const success = await handleSave(next)
    if (!success) onChange(education)
  }
  const { reorderFor } = useDragReorder(education, onChange, persistReorder, isSaving)

  return (
    <Card
      id="education"
      icon="graduation-cap"
      heading="Education and training"
      meta={education.length > 0 ? `${education.length} entr${education.length === 1 ? 'y' : 'ies'}` : undefined}
      actions={
        <Button type="button" variant="success" size="sm" onClick={addEntry} disabled={isSaving}>
          + Add education or a course
        </Button>
      }
    >
      {error && (
        <p role="alert" className={styles.error}>
          {error}
        </p>
      )}

      {education.length === 0 ? (
        <EmptyState title="No education recorded yet" description="Degrees and courses live together — a CV decides what to show." />
      ) : (
        <div className={styles.rows}>
          {education.map((entry, index) => {
            const isEditing = editingIds.has(entry.id)
            return (
              <CollapsibleRow
                key={entry.id}
                ordinal={index + 1}
                open={openIds.has(entry.id)}
                onToggle={() => toggleOpen(entry.id)}
                reorder={reorderFor(entry.id, entry.degree || 'this entry')}
                title={entry.degree || 'Untitled qualification'}
                subtitle={entry.institution || undefined}
                meta={entry.startDate ? formatDateRange(entry.startDate, entry.endDate) : undefined}
              >
                <div className={styles.moveRow}>
                  <Button type="button" variant="ghost" onClick={() => void moveTo(entry.id, -1)} disabled={isSaving || index === 0} aria-label={`Move ${entry.degree || 'entry'} up`}>
                    Move up
                  </Button>
                  <Button
                    type="button"
                    variant="ghost"
                    onClick={() => void moveTo(entry.id, 1)}
                    disabled={isSaving || index === education.length - 1}
                    aria-label={`Move ${entry.degree || 'entry'} down`}
                  >
                    Move down
                  </Button>
                </div>
                {isEditing ? (
                  <>
                    <EducationEntryFields value={entry} onChange={(next) => onChange(education.map((item) => (item.id === entry.id ? next : item)))} />
                    <div className={styles.rowActions}>
                      <span className={styles.rowActionsSpacer} />
                      <Button type="button" variant="danger" onClick={() => void removeEntry(entry.id)} disabled={isSaving}>
                        Delete
                      </Button>
                      <Button type="button" variant="success" onClick={() => void stopEditingAndSave(entry.id)} isLoading={isSaving}>
                        Done
                      </Button>
                    </div>
                  </>
                ) : (
                  <>
                    <EducationEntryReadView value={entry} />
                    <div className={styles.rowActions}>
                      <span className={styles.rowActionsSpacer} />
                      <Button type="button" variant="danger" onClick={() => void removeEntry(entry.id)} disabled={isSaving}>
                        Delete
                      </Button>
                      <Button type="button" variant="accent" onClick={() => setEditing(entry.id, true)} disabled={isSaving}>
                        Edit
                      </Button>
                    </div>
                  </>
                )}
              </CollapsibleRow>
            )
          })}
        </div>
      )}
    </Card>
  )
}
