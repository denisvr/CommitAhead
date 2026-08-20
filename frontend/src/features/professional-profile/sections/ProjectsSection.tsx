import { useState } from 'react'
import { Badge } from '../../../design-system/components/Badge'
import { Button } from '../../../design-system/components/Button'
import { Card } from '../../../design-system/components/Card'
import { Chip } from '../../../design-system/components/Chip'
import { CollapsibleRow } from '../../../design-system/components/CollapsibleRow'
import { EmptyState } from '../../../design-system/components/EmptyState'
import { RestrictedMarkdown } from '../../../design-system/components/RestrictedMarkdown'
import { SafeLink } from '../../../design-system/components/SafeLink'
import { replaceProjects, type ProjectEntryDto, type SkillDto } from '../api'
import { ProjectEntryFields } from '../fields/ProjectEntryFields'
import { useDragReorder } from '../useDragReorder'
import { useSectionSave } from '../useEditableCollection'
import styles from './sections.module.css'

type ProjectsSectionProps = {
  projects: ProjectEntryDto[]
  skills: SkillDto[]
  onChange: (projects: ProjectEntryDto[]) => void
}

const createEntry = (): ProjectEntryDto => ({ id: crypto.randomUUID(), name: '', role: null, startDate: null, endDate: null, descriptionMarkdown: '', url: null, skillIds: [] })

function ProjectEntryReadView({ value, skills }: { value: ProjectEntryDto; skills: SkillDto[] }) {
  const byId = new Map(skills.map((skill) => [skill.id, skill.displayName]))
  const skillNames = value.skillIds.map((id) => byId.get(id)).filter((name): name is string => Boolean(name))

  return (
    <div className={styles.readView}>
      {value.url && (
        <SafeLink url={value.url}>
          <span className={styles.readLink}>{value.url}</span>
        </SafeLink>
      )}
      {value.descriptionMarkdown.trim() !== '' ? (
        <RestrictedMarkdown className={styles.readSummary}>{value.descriptionMarkdown}</RestrictedMarkdown>
      ) : (
        <p className={styles.readEmpty}>No description recorded yet.</p>
      )}
      {skillNames.length > 0 && (
        <div className={styles.readChips}>
          {skillNames.map((name) => (
            <Chip key={name}>{name}</Chip>
          ))}
        </div>
      )}
    </div>
  )
}

// Projects are entirely optional — most engineers with a full employment history have one or
// none, so this never carries a caution/critical badge, only a neutral count.
export function ProjectsSection({ projects, skills, onChange }: ProjectsSectionProps) {
  const { error, isSaving, handleSave } = useSectionSave(projects, replaceProjects)
  const [openIds, setOpenIds] = useState<Set<string>>(() => new Set(projects.length === 1 ? [projects[0].id] : []))
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
    onChange([entry, ...projects])
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
    const previous = projects
    const next = projects.filter((item) => item.id !== id)
    onChange(next)
    const success = await handleSave(next)
    if (!success) onChange(previous)
  }

  // Applies the reorder optimistically but reverts to the pre-reorder array if the PUT fails — an
  // optimistic update must never linger as if it had been saved.
  const moveTo = async (id: string, delta: number) => {
    const index = projects.findIndex((entry) => entry.id === id)
    const target = index + delta
    if (index < 0 || target < 0 || target >= projects.length) return
    const previous = projects
    const next = [...projects]
    ;[next[index], next[target]] = [next[target], next[index]]
    onChange(next)
    const success = await handleSave(next)
    if (!success) onChange(previous)
  }

  const persistReorder = async (next: ProjectEntryDto[]) => {
    const success = await handleSave(next)
    if (!success) onChange(projects)
  }
  const { reorderFor } = useDragReorder(projects, onChange, persistReorder, isSaving)

  return (
    <Card
      id="projects"
      icon="rocket"
      heading="Projects"
      meta={`${projects.length} project${projects.length === 1 ? '' : 's'}`}
      badge={<Badge tone="neutral">Optional</Badge>}
      actions={
        <Button type="button" variant="success" size="sm" onClick={addEntry} disabled={isSaving}>
          + Add a project
        </Button>
      }
    >
      {error && (
        <p role="alert" className={styles.error}>
          {error}
        </p>
      )}

      {projects.length === 0 ? (
        <EmptyState title="No projects yet" description="Entirely optional — add one when it shows something your jobs don't." />
      ) : (
        <div className={styles.rows}>
          {projects.map((entry, index) => {
            const isEditing = editingIds.has(entry.id)
            return (
              <CollapsibleRow
                key={entry.id}
                ordinal={index + 1}
                open={openIds.has(entry.id)}
                onToggle={() => toggleOpen(entry.id)}
                reorder={reorderFor(entry.id, entry.name || 'this project')}
                title={entry.name || 'Untitled project'}
                subtitle={entry.role || undefined}
              >
                <div className={styles.moveRow}>
                  <Button type="button" variant="ghost" onClick={() => void moveTo(entry.id, -1)} disabled={isSaving || index === 0} aria-label={`Move ${entry.name || 'entry'} up`}>
                    Move up
                  </Button>
                  <Button
                    type="button"
                    variant="ghost"
                    onClick={() => void moveTo(entry.id, 1)}
                    disabled={isSaving || index === projects.length - 1}
                    aria-label={`Move ${entry.name || 'entry'} down`}
                  >
                    Move down
                  </Button>
                </div>
                {isEditing ? (
                  <>
                    <ProjectEntryFields value={entry} onChange={(next) => onChange(projects.map((item) => (item.id === entry.id ? next : item)))} skills={skills} />
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
                    <ProjectEntryReadView value={entry} skills={skills} />
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
