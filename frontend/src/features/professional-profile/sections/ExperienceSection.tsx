import { useEffect, useState, type DragEvent } from 'react'
import { Badge } from '../../../design-system/components/Badge'
import { Button } from '../../../design-system/components/Button'
import { Card } from '../../../design-system/components/Card'
import { Chip } from '../../../design-system/components/Chip'
import { CollapsibleRow } from '../../../design-system/components/CollapsibleRow'
import { EmptyState } from '../../../design-system/components/EmptyState'
import { RestrictedMarkdown } from '../../../design-system/components/RestrictedMarkdown'
import { replaceExperience, type ExperienceEntryDto, type SkillDto } from '../api'
import { ExperienceEntryFields } from '../fields/ExperienceEntryFields'
import { compareStartDateDesc, formatDateRange, formatDuration, totalDuration } from '../formatDate'
import { useDragReorder } from '../useDragReorder'
import { useSectionSave } from '../useEditableCollection'
import styles from './sections.module.css'

type SortMode = 'newest' | 'oldest' | 'manual'

// A new object on every request (not just the id) so clicking the same experience twice in the
// preview — open it, close it by hand, click it again — reliably reopens it: an effect keyed on
// this value fires on every click, where one keyed only on the id string would not.
export type FocusRequest = { id: string; token: number }

type ExperienceSectionProps = {
  experience: ExperienceEntryDto[]
  skills: SkillDto[]
  onChange: (experience: ExperienceEntryDto[]) => void
  focusRequest: FocusRequest | null
  onHighlightAchievement: (experienceId: string, index: number | null) => void
}

const createEntry = (): ExperienceEntryDto => ({
  id: crypto.randomUUID(),
  company: '',
  client: null,
  role: '',
  employmentType: 'Permanent',
  startDate: { year: new Date().getFullYear(), month: 1 },
  endDate: null,
  location: null,
  workMode: 'Remote',
  summaryMarkdown: '',
  achievements: [],
  skillIds: [],
})

// Europass-style read view for an open-but-not-editing row: formatted text, not input fields.
// Employment type/work mode and dates/location already show in the row's own header, so this
// covers only what the header leaves out.
function ExperienceEntryReadView({ value, skills }: { value: ExperienceEntryDto; skills: SkillDto[] }) {
  const byId = new Map(skills.map((skill) => [skill.id, skill.displayName]))
  const skillNames = value.skillIds.map((id) => byId.get(id)).filter((name): name is string => Boolean(name))

  return (
    <div className={styles.readView}>
      <p className={styles.readMetaLine}>
        {value.employmentType} · {value.workMode}
      </p>
      {value.summaryMarkdown.trim() !== '' && <RestrictedMarkdown className={styles.readSummary}>{value.summaryMarkdown}</RestrictedMarkdown>}
      {value.achievements.length > 0 ? (
        <ul className={styles.readList}>
          {value.achievements.map((achievement, index) => (
            <li key={index}>{achievement}</li>
          ))}
        </ul>
      ) : (
        <p className={styles.readEmpty}>No achievements recorded yet.</p>
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

// `experience` lives in the parent's state (ProfessionalProfilePage), not a local draft here —
// onChange fires on every edit so the profile preview shows what is being typed, and Save just
// persists whatever the parent currently holds.
export function ExperienceSection({ experience, skills, onChange, focusRequest, onHighlightAchievement }: ExperienceSectionProps) {
  const { error, isSaving, handleSave } = useSectionSave(experience, replaceExperience)
  const [sortMode, setSortMode] = useState<SortMode>('newest')
  const [openIds, setOpenIds] = useState<Set<string>>(new Set())
  // Read by default once opened (Europass-style) — a row only shows its edit form once the user
  // asks for it via "Edit", not just from being expanded.
  const [editingIds, setEditingIds] = useState<Set<string>>(new Set())
  const [processedToken, setProcessedToken] = useState<number | null>(null)

  // Render-phase state adjustment (react.dev "Adjusting state when a prop changes"), not an
  // effect — react-hooks/set-state-in-effect forbids calling setState synchronously inside an
  // effect body. Guarded by the token so it fires exactly once per focus request rather than on
  // every unrelated re-render, which is what a plain "if row isn't open, open it" check would do
  // and would reopen a row the user had just closed by hand.
  if (focusRequest && focusRequest.token !== processedToken) {
    setProcessedToken(focusRequest.token)
    setOpenIds((current) => new Set(current).add(focusRequest.id))
  }

  useEffect(() => {
    if (!focusRequest) return
    document.getElementById(`experience-${focusRequest.id}`)?.scrollIntoView?.({ behavior: 'smooth', block: 'center' })
  }, [focusRequest])

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

  const ordered =
    sortMode === 'manual' ? experience : [...experience].sort((a, b) => (sortMode === 'newest' ? compareStartDateDesc(a, b) : -compareStartDateDesc(a, b)))

  // Applies the reorder optimistically (so the UI feels instant) but reverts to the pre-reorder
  // array if the PUT fails — an optimistic update must never linger as if it had been saved.
  const moveTo = async (id: string, delta: number) => {
    const index = experience.findIndex((entry) => entry.id === id)
    const target = index + delta
    if (index < 0 || target < 0 || target >= experience.length) return
    const previous = experience
    const next = [...experience]
    ;[next[index], next[target]] = [next[target], next[index]]
    onChange(next)
    const success = await handleSave(next)
    if (!success) onChange(previous)
  }

  const persistReorder = async (next: ExperienceEntryDto[]) => {
    const success = await handleSave(next)
    if (!success) onChange(experience)
  }
  const dragReorder = useDragReorder(experience, onChange, persistReorder, isSaving)
  // Available regardless of sortMode (unlike the Move up/down buttons, which only make sense once
  // already in Manual) — starting a drag is itself an unambiguous request for manual order, so it
  // switches the dropdown there rather than requiring that as a separate step first.
  const reorderFor = (id: string, label: string) => {
    const base = dragReorder.reorderFor(id, label)
    return {
      ...base,
      onHandleDragStart: (event: DragEvent<HTMLSpanElement>) => {
        if (isSaving) return
        setSortMode('manual')
        base.onHandleDragStart(event)
      },
    }
  }

  const addEntry = () => {
    const entry = createEntry()
    onChange([entry, ...experience])
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
    const previous = experience
    const next = experience.filter((item) => item.id !== id)
    onChange(next)
    const success = await handleSave(next)
    if (!success) onChange(previous)
  }

  const missingImpact = experience.filter((entry) => entry.achievements.length === 0).length
  const totalYears = experience.length > 0 ? totalDuration(experience) : null

  return (
    <Card
      id="experience"
      icon="briefcase"
      heading="Experience"
      meta={experience.length > 0 ? `${experience.length} position${experience.length === 1 ? '' : 's'} · ${totalYears} combined` : undefined}
      badge={missingImpact > 0 ? <Badge tone="caution">{missingImpact} have no impact yet</Badge> : undefined}
      lead="Your full employment record. Depth here is an asset — a CV for a solution architect role and a CV for an engineering manager role pull different things out of the same history."
      actions={
        <Button type="button" variant="success" size="sm" onClick={addEntry} disabled={isSaving}>
          + Add a position
        </Button>
      }
    >
      {error && (
        <p role="alert" className={styles.error}>
          {error}
        </p>
      )}

      {experience.length > 1 && (
        <div className={styles.ord}>
          <label htmlFor="experience-order">Order</label>
          <select id="experience-order" className={styles.ordSelect} value={sortMode} onChange={(event) => setSortMode(event.target.value as SortMode)}>
            <option value="newest">Newest first (recommended)</option>
            <option value="oldest">Oldest first</option>
            <option value="manual">Manual</option>
          </select>
          <span className={styles.ordNote}>
            {sortMode === 'manual' ? 'Manual order applies to this profile view only.' : 'Chronological order is kept for you. Each CV decides its own order.'}
          </span>
        </div>
      )}

      {ordered.length === 0 ? (
        <EmptyState title="No experience recorded yet" description="Add every position — a CV will later choose what to show for a given role." />
      ) : (
        <div className={styles.rows}>
          {ordered.map((entry, position) => {
            const index = experience.findIndex((item) => item.id === entry.id)
            const isEditing = editingIds.has(entry.id)
            return (
              <CollapsibleRow
                key={entry.id}
                id={`experience-${entry.id}`}
                ordinal={position + 1}
                open={openIds.has(entry.id)}
                onToggle={() => toggleOpen(entry.id)}
                reorder={reorderFor(entry.id, entry.role || 'this position')}
                title={entry.role || 'Untitled role'}
                subtitle={[entry.company, entry.client].filter(Boolean).join(' · client: ') || undefined}
                meta={`${formatDateRange(entry.startDate, entry.endDate)} · ${formatDuration(entry.startDate, entry.endDate)}${entry.location ? ` · ${entry.location}` : ''}`}
                status={
                  entry.achievements.length === 0 ? (
                    <Badge tone="caution" size="sm">
                      No impact yet
                    </Badge>
                  ) : (
                    <span className={styles.qty}>
                      {entry.achievements.length} achievement{entry.achievements.length === 1 ? '' : 's'}
                    </span>
                  )
                }
              >
                {sortMode === 'manual' && (
                  <div className={styles.moveRow}>
                    <Button type="button" variant="ghost" onClick={() => void moveTo(entry.id, -1)} disabled={isSaving || index === 0} aria-label={`Move ${entry.role || 'entry'} up`}>
                      Move up
                    </Button>
                    <Button
                      type="button"
                      variant="ghost"
                      onClick={() => void moveTo(entry.id, 1)}
                      disabled={isSaving || index === experience.length - 1}
                      aria-label={`Move ${entry.role || 'entry'} down`}
                    >
                      Move down
                    </Button>
                  </div>
                )}
                {isEditing ? (
                  <>
                    <ExperienceEntryFields
                      value={entry}
                      onChange={(next) => onChange(experience.map((item) => (item.id === entry.id ? next : item)))}
                      skills={skills}
                      onHighlightAchievement={(achievementIndex) => onHighlightAchievement(entry.id, achievementIndex)}
                    />
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
                    <ExperienceEntryReadView value={entry} skills={skills} />
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
