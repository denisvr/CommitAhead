import { useState } from 'react'
import { Badge } from '../../../design-system/components/Badge'
import { Button } from '../../../design-system/components/Button'
import { Card } from '../../../design-system/components/Card'
import { Field } from '../../../design-system/components/Field'
import { RestrictedMarkdown } from '../../../design-system/components/RestrictedMarkdown'
import inputStyles from '../../../design-system/components/Input.module.css'
import { updateProfessionalProfile, type ContactInfoDto } from '../api'
import layout from '../FormLayout.module.css'
import styles from './AboutYouSection.module.css'
import sectionStyles from './sections.module.css'

type AboutYouSectionProps = {
  contactInfo: ContactInfoDto
  summaryMarkdown: string
  onChange: (contactInfo: ContactInfoDto, summaryMarkdown: string) => void
}

function initialsOf(name: string): string {
  const words = name.trim().split(/\s+/).filter(Boolean)
  if (words.length === 0) return '?'
  if (words.length === 1) return words[0].slice(0, 2).toUpperCase()
  return (words[0][0] + words[words.length - 1][0]).toUpperCase()
}

// One labelled row per field, mirroring the edit-mode Field's own label styling (same size/weight/
// colour) so toggling between read and edit doesn't jump — just swaps a value for an input.
function ReadField({ label, value, className }: { label: string; value: string; className?: string }) {
  return (
    <div className={[styles.readFieldBlock, className].filter(Boolean).join(' ')}>
      <span className={styles.readFieldLabel}>{label}</span>
      <span className={styles.readFieldValue}>{value || '—'}</span>
    </div>
  )
}

// contactInfo/summaryMarkdown live in the parent's state, not a local draft — onChange fires on
// every keystroke so the profile preview shows the name and summary as they are being typed.
//
// Fields render directly (not via ContactInfoFields, which stays for the separate create-profile
// bootstrap screen) because the approved layout puts every field — including the summary — in
// one grid alongside the avatar, not in a nested sub-form.
export function AboutYouSection({ contactInfo, summaryMarkdown, onChange }: AboutYouSectionProps) {
  // Starts in edit mode only when there's nothing meaningful to read yet (Europass-style: text
  // first, edit on request) — otherwise a brand-new profile would show an empty read view with no
  // way to tell how to fill it in.
  const [isEditing, setIsEditing] = useState(contactInfo.name.trim() === '')
  const [isSaving, setIsSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const complete = contactInfo.name.trim() !== '' && contactInfo.email.trim() !== '' && summaryMarkdown.trim() !== ''

  // No separate Save button — Done persists and exits edit mode in one action.
  const stopEditingAndSave = async () => {
    setIsSaving(true)
    setError(null)
    try {
      await updateProfessionalProfile(contactInfo, summaryMarkdown)
      setIsEditing(false)
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : 'Something went wrong saving your contact info and summary.')
    } finally {
      setIsSaving(false)
    }
  }

  return (
    <Card
      id="about-you"
      icon="user-round"
      heading="About you"
      meta="Identity, contact details and how you describe yourself"
      badge={complete ? <Badge tone="good">Complete</Badge> : <Badge tone="caution">Add your details</Badge>}
    >
      <div className={styles.hero}>
        <div className={styles.avatar}>{initialsOf(contactInfo.name)}</div>

        {isEditing ? (
          <div className={layout.grid}>
            <Field label="Full name" required>
              {(fieldProps) => <input {...fieldProps} type="text" className={inputStyles.input} value={contactInfo.name} onChange={(event) => onChange({ ...contactInfo, name: event.target.value }, summaryMarkdown)} />}
            </Field>
            <Field label="Email" required>
              {(fieldProps) => (
                <input {...fieldProps} type="email" className={inputStyles.input} value={contactInfo.email} onChange={(event) => onChange({ ...contactInfo, email: event.target.value }, summaryMarkdown)} />
              )}
            </Field>
            <Field label="Phone">
              {(fieldProps) => (
                <input
                  {...fieldProps}
                  type="tel"
                  className={inputStyles.input}
                  value={contactInfo.phone ?? ''}
                  onChange={(event) => onChange({ ...contactInfo, phone: event.target.value || null }, summaryMarkdown)}
                />
              )}
            </Field>
            <Field label="Address">
              {(fieldProps) => (
                <input
                  {...fieldProps}
                  type="text"
                  className={inputStyles.input}
                  value={contactInfo.address ?? ''}
                  onChange={(event) => onChange({ ...contactInfo, address: event.target.value || null }, summaryMarkdown)}
                />
              )}
            </Field>
            <Field label="Photo storage key" inlineHint="a raw storage reference, not an upload widget yet">
              {(fieldProps) => (
                <input
                  {...fieldProps}
                  type="text"
                  className={inputStyles.input}
                  value={contactInfo.photoStorageKey ?? ''}
                  onChange={(event) => onChange({ ...contactInfo, photoStorageKey: event.target.value || null }, summaryMarkdown)}
                />
              )}
            </Field>
            <Field label="Professional summary" inlineHint="each CV shortens it for the role" hint={`${summaryMarkdown.length} characters`} className={layout.wide}>
              {(fieldProps) => <textarea {...fieldProps} className={inputStyles.input} value={summaryMarkdown} onChange={(event) => onChange(contactInfo, event.target.value)} />}
            </Field>
          </div>
        ) : (
          <div className={layout.grid}>
            <ReadField label="Full name" value={contactInfo.name} />
            <ReadField label="Email" value={contactInfo.email} />
            <ReadField label="Phone" value={contactInfo.phone ?? ''} />
            <ReadField label="Address" value={contactInfo.address ?? ''} />
            <div className={[styles.readFieldBlock, layout.wide].join(' ')}>
              <span className={styles.readFieldLabel}>Professional summary</span>
              {summaryMarkdown.trim() !== '' ? (
                <RestrictedMarkdown className={sectionStyles.readSummary}>{summaryMarkdown}</RestrictedMarkdown>
              ) : (
                <span className={styles.readFieldValue}>—</span>
              )}
            </div>
          </div>
        )}
      </div>

      <div className={sectionStyles.rowActions}>
        <span className={sectionStyles.rowActionsSpacer} />
        {error && <p className={sectionStyles.error}>{error}</p>}
        {isEditing ? (
          <Button type="button" variant="success" onClick={stopEditingAndSave} isLoading={isSaving}>
            Done
          </Button>
        ) : (
          <Button type="button" variant="accent" onClick={() => setIsEditing(true)}>
            Edit
          </Button>
        )}
      </div>
    </Card>
  )
}
