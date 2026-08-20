import { useState } from 'react'
import { Badge } from '../../../design-system/components/Badge'
import { Button } from '../../../design-system/components/Button'
import { Card } from '../../../design-system/components/Card'
import { CollapsibleRow } from '../../../design-system/components/CollapsibleRow'
import { EmptyState } from '../../../design-system/components/EmptyState'
import { SafeLink } from '../../../design-system/components/SafeLink'
import { replaceCertifications, type CertificationEntryDto } from '../api'
import { CertificationEntryFields } from '../fields/CertificationEntryFields'
import { formatMonthYear } from '../formatDate'
import { useDragReorder } from '../useDragReorder'
import { useSectionSave } from '../useEditableCollection'
import styles from './sections.module.css'

type CertificationsSectionProps = {
  certifications: CertificationEntryDto[]
  onChange: (certifications: CertificationEntryDto[]) => void
}

const createEntry = (): CertificationEntryDto => ({ id: crypto.randomUUID(), name: '', issuingOrganisation: '', issuedAt: null, expiresAt: null, credentialId: null, url: null })

function CertificationEntryReadView({ value }: { value: CertificationEntryDto }) {
  const metaBits = [value.expiresAt ? `Expires ${formatMonthYear(value.expiresAt)}` : null, value.credentialId ? `Credential ID: ${value.credentialId}` : null].filter(Boolean)
  return (
    <div className={styles.readView}>
      {metaBits.length > 0 && <p className={styles.readMetaLine}>{metaBits.join(' · ')}</p>}
      {value.url ? (
        <SafeLink url={value.url}>
          <span className={styles.readLink}>{value.url}</span>
        </SafeLink>
      ) : (
        <p className={styles.readEmpty}>No verification link yet.</p>
      )}
    </div>
  )
}

export function CertificationsSection({ certifications, onChange }: CertificationsSectionProps) {
  const { error, isSaving, handleSave } = useSectionSave(certifications, replaceCertifications)
  const [openIds, setOpenIds] = useState<Set<string>>(() => new Set(certifications.length === 1 ? [certifications[0].id] : []))
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
    onChange([entry, ...certifications])
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
    const previous = certifications
    const next = certifications.filter((item) => item.id !== id)
    onChange(next)
    const success = await handleSave(next)
    if (!success) onChange(previous)
  }

  // Applies the reorder optimistically but reverts to the pre-reorder array if the PUT fails — an
  // optimistic update must never linger as if it had been saved.
  const moveTo = async (id: string, delta: number) => {
    const index = certifications.findIndex((entry) => entry.id === id)
    const target = index + delta
    if (index < 0 || target < 0 || target >= certifications.length) return
    const previous = certifications
    const next = [...certifications]
    ;[next[index], next[target]] = [next[target], next[index]]
    onChange(next)
    const success = await handleSave(next)
    if (!success) onChange(previous)
  }

  const persistReorder = async (next: CertificationEntryDto[]) => {
    const success = await handleSave(next)
    if (!success) onChange(certifications)
  }
  const { reorderFor } = useDragReorder(certifications, onChange, persistReorder, isSaving)

  const missingLink = certifications.filter((entry) => !entry.url).length

  return (
    <Card
      id="certifications"
      icon="award"
      heading="Certifications"
      meta={certifications.length > 0 ? `${certifications.length} certification${certifications.length === 1 ? '' : 's'}` : undefined}
      badge={missingLink > 0 ? <Badge tone="caution">{missingLink} links missing</Badge> : undefined}
      actions={
        <Button type="button" variant="success" size="sm" onClick={addEntry} disabled={isSaving}>
          + Add a certification
        </Button>
      }
    >
      {error && (
        <p role="alert" className={styles.error}>
          {error}
        </p>
      )}

      {certifications.length === 0 ? (
        <EmptyState title="No certifications recorded yet" description="Add anything with a name and an issuer." />
      ) : (
        <div className={styles.rows}>
          {certifications.map((entry, index) => {
            const isEditing = editingIds.has(entry.id)
            return (
              <CollapsibleRow
                key={entry.id}
                ordinal={index + 1}
                open={openIds.has(entry.id)}
                onToggle={() => toggleOpen(entry.id)}
                reorder={reorderFor(entry.id, entry.name || 'this certification')}
                title={entry.name || 'Untitled certification'}
                subtitle={entry.issuingOrganisation || undefined}
                meta={entry.issuedAt ? `Issued ${formatMonthYear(entry.issuedAt)}` : undefined}
                status={
                  !entry.url ? (
                    <Badge tone="caution" size="sm">
                      Add link
                    </Badge>
                  ) : undefined
                }
              >
                <div className={styles.moveRow}>
                  <Button type="button" variant="ghost" onClick={() => void moveTo(entry.id, -1)} disabled={isSaving || index === 0} aria-label={`Move ${entry.name || 'entry'} up`}>
                    Move up
                  </Button>
                  <Button
                    type="button"
                    variant="ghost"
                    onClick={() => void moveTo(entry.id, 1)}
                    disabled={isSaving || index === certifications.length - 1}
                    aria-label={`Move ${entry.name || 'entry'} down`}
                  >
                    Move down
                  </Button>
                </div>
                {isEditing ? (
                  <>
                    <CertificationEntryFields value={entry} onChange={(next) => onChange(certifications.map((item) => (item.id === entry.id ? next : item)))} />
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
                    <CertificationEntryReadView value={entry} />
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
