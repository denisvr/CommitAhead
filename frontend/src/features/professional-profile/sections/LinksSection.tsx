import { useState } from 'react'
import { Button } from '../../../design-system/components/Button'
import { Card } from '../../../design-system/components/Card'
import { Chip } from '../../../design-system/components/Chip'
import { EmptyState } from '../../../design-system/components/EmptyState'
import { replaceProfileLinks, type ProfileLinkDto } from '../api'
import { ProfileLinkFields } from '../fields/ProfileLinkFields'
import { useSectionSave } from '../useEditableCollection'
import styles from './sections.module.css'

type LinksSectionProps = {
  profileLinks: ProfileLinkDto[]
  onChange: (profileLinks: ProfileLinkDto[]) => void
}

const createEntry = (): ProfileLinkDto => ({ id: crypto.randomUUID(), kind: 'Other', label: null, url: '' })

// Chips are the reference's own collapsed shape for a link (".chip") — but its mock only ever
// removes one, never edits a URL through it. Editing needs real Kind/Label/URL fields, so clicking
// a chip (not its ✕) expands ProfileLinkFields beneath the chip row, echoing the CollapsibleRow
// pattern used for Experience/Education rather than inventing a new interaction from nothing.
export function LinksSection({ profileLinks, onChange }: LinksSectionProps) {
  const { error, handleSave } = useSectionSave(profileLinks, replaceProfileLinks)
  const [expandedId, setExpandedId] = useState<string | null>(null)

  const addEntry = () => {
    const entry = createEntry()
    onChange([...profileLinks, entry])
    setExpandedId(entry.id)
  }

  // No separate Save button — Done persists the whole current list immediately, since the API
  // only offers whole-collection replace; the "per item" feel comes from the UI action, not a
  // per-entity endpoint.
  const stopEditingAndSave = () => {
    setExpandedId(null)
    void handleSave()
  }

  const removeEntry = (id: string) => {
    const next = profileLinks.filter((item) => item.id !== id)
    onChange(next)
    setExpandedId((current) => (current === id ? null : current))
    void handleSave(next)
  }

  const expanded = profileLinks.find((entry) => entry.id === expandedId) ?? null

  return (
    <Card
      id="links"
      icon="link"
      heading="Links"
      meta={`${profileLinks.length} link${profileLinks.length === 1 ? '' : 's'}`}
      actions={
        <Button type="button" variant="success" size="sm" onClick={addEntry}>
          + Add link
        </Button>
      }
    >
      {error && (
        <p role="alert" className={styles.error}>
          {error}
        </p>
      )}

      {profileLinks.length === 0 && <EmptyState title="No links yet" description="LinkedIn, GitHub, a portfolio — each CV picks which of these to print." />}

      <div className={styles.chips}>
        {profileLinks.map((entry) => (
          <Chip
            key={entry.id}
            onClick={() => setExpandedId((current) => (current === entry.id ? null : entry.id))}
            onRemove={() => removeEntry(entry.id)}
            removeLabel={`Remove ${entry.label || entry.kind}`}
          >
            {entry.label || entry.kind}
          </Chip>
        ))}
      </div>

      {expanded && (
        <div className={styles.chipEditor}>
          <ProfileLinkFields value={expanded} onChange={(next) => onChange(profileLinks.map((item) => (item.id === expanded.id ? next : item)))} />
          <div className={styles.rowActions}>
            <span className={styles.rowActionsSpacer} />
            <Button type="button" variant="danger" onClick={() => removeEntry(expanded.id)}>
              Delete
            </Button>
            <Button type="button" variant="success" onClick={stopEditingAndSave}>
              Done
            </Button>
          </div>
        </div>
      )}
    </Card>
  )
}
