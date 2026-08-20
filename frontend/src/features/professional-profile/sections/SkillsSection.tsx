import { Fragment, useState } from 'react'
import { Button } from '../../../design-system/components/Button'
import { Card } from '../../../design-system/components/Card'
import { EmptyState } from '../../../design-system/components/EmptyState'
import { replaceSkills, type SkillCategory, type SkillDto } from '../api'
import { SkillFields } from '../fields/SkillFields'
import { useSectionSave } from '../useEditableCollection'
import styles from './sections.module.css'

type SkillsSectionProps = {
  skills: SkillDto[]
  onChange: (skills: SkillDto[]) => void
}

// The fixed backend taxonomy (13 values) doubles as the "group" the prototype envisioned as
// free-form — this shows the real attribute rather than inventing custom group names the model
// does not have. A future slice could let a skill carry a user-defined group on top of this.
const CATEGORY_ORDER: SkillCategory[] = [
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

const createEntry = (category: SkillCategory): SkillDto => ({ id: crypto.randomUUID(), displayName: '', normalizedKey: '', category, proficiency: null })

// One shared table for the whole card, not one per group — a separate <table> per group each
// computed its own column widths from only its own rows, so Category/Proficiency drifted out of
// alignment between groups. Group headings are colSpan divider rows inside the one table/tbody
// instead, so every row (any group) shares the same column layout.
export function SkillsSection({ skills, onChange }: SkillsSectionProps) {
  const { error, isSaving, handleSave } = useSectionSave(skills, replaceSkills)
  const [editingIds, setEditingIds] = useState<Set<string>>(new Set())

  const setEditing = (id: string, value: boolean) => {
    const next = new Set(editingIds)
    if (value) next.add(id)
    else next.delete(id)
    setEditingIds(next)
  }

  const addEntry = () => {
    const entry = createEntry('Other')
    onChange([...skills, entry])
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
    const previous = skills
    const next = skills.filter((item) => item.id !== id)
    onChange(next)
    const success = await handleSave(next)
    if (!success) onChange(previous)
  }

  const groups = CATEGORY_ORDER.map((category) => ({ category, entries: skills.filter((skill) => skill.category === category) })).filter((group) => group.entries.length > 0)

  return (
    <Card
      id="skills"
      icon="wrench"
      heading="Skills"
      meta={`${skills.length} skill${skills.length === 1 ? '' : 's'}${groups.length > 0 ? ` in ${groups.length} group${groups.length === 1 ? '' : 's'}` : ''}`}
      actions={
        <Button type="button" variant="success" size="sm" onClick={addEntry} disabled={isSaving}>
          + Add a skill
        </Button>
      }
    >
      {error && (
        <p role="alert" className={styles.error}>
          {error}
        </p>
      )}

      {skills.length === 0 ? (
        <EmptyState title="No skills yet" description="Add what you actually use — level and category help a future CV choose what to lead with." />
      ) : (
        <table className={styles.skillsTable}>
          <thead>
            <tr>
              <th>Skill</th>
              <th>Category</th>
              <th>Proficiency</th>
              <th className={styles.skillActionCell} />
            </tr>
          </thead>
          <tbody>
            {groups.map(({ category, entries }) => (
              <Fragment key={category}>
                <tr className={styles.skillGroupRow}>
                  <td colSpan={4}>
                    <span className={styles.groupTitle}>{category}</span> <span className={styles.groupCount}>{entries.length}</span>
                  </td>
                </tr>
                {entries.map((entry) => (
                  <SkillFields
                    key={entry.id}
                    value={entry}
                    isEditing={editingIds.has(entry.id)}
                    disabled={isSaving}
                    onChange={(next) => onChange(skills.map((item) => (item.id === entry.id ? next : item)))}
                    onRemove={() => void removeEntry(entry.id)}
                    onStartEdit={() => setEditing(entry.id, true)}
                    onStopEdit={() => void stopEditingAndSave(entry.id)}
                  />
                ))}
              </Fragment>
            ))}
          </tbody>
        </table>
      )}
    </Card>
  )
}
