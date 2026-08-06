import { useEffect, useState } from 'react'
import { Button } from '../../design-system/components/Button'
import { Icon } from '../../design-system/Icon'
import { SelectionOrderEditor } from '../../design-system/components/SelectionOrderEditor'
import { Tabs } from '../../design-system/components/Tabs'
import { fetchProfessionalProfile, type ProfessionalProfileResponse } from '../professional-profile/api'
import {
  deleteCVPresentation,
  fetchCVPresentation,
  replaceCertificationSelections,
  replaceEducationSelections,
  replaceExperienceSelections,
  replaceLanguageSelections,
  replaceProfileLinkSelections,
  replaceProjectSelections,
  replaceSkillSelections,
  type CVPresentationResponse,
} from './api'
import { CVPresentationForm } from './CVPresentationForm'
import { CVPreview } from './CVPreview'
import styles from './CVPresentationDetailPage.module.css'

type LoadState = 'loading' | 'not-found' | 'ready' | 'error'

type CVPresentationDetailPageProps = {
  presentationId: string
  onBack: () => void
  onDeleted: () => void
}

function describeError(caught: unknown, fallback: string): string {
  return caught instanceof Error ? caught.message : fallback
}

type SelectionSectionProps<T> = {
  title: string
  candidates: T[]
  selectedIds: string[]
  onSaved: (selectedIds: string[]) => void
  getId: (candidate: T) => string
  getLabel: (candidate: T) => string
  addLabel: string
  emptyLabel: string
  save: (id: string, entryIds: string[]) => Promise<void>
  presentationId: string
}

function SelectionSection<T>({ title, candidates, selectedIds, onSaved, getId, getLabel, addLabel, emptyLabel, save, presentationId }: SelectionSectionProps<T>) {
  const [error, setError] = useState<string | null>(null)
  const [isSaving, setIsSaving] = useState(false)

  const handleChange = async (nextIds: string[]) => {
    setIsSaving(true)
    setError(null)
    try {
      await save(presentationId, nextIds)
      onSaved(nextIds)
    } catch (caught) {
      setError(describeError(caught, 'Something went wrong saving this selection.'))
    } finally {
      setIsSaving(false)
    }
  }

  return (
    <div className={styles.section}>
      <h3 className={styles.sectionTitle}>{title}</h3>
      <SelectionOrderEditor
        candidates={candidates}
        selectedIds={selectedIds}
        onChange={(next) => void handleChange(next)}
        getId={getId}
        getLabel={getLabel}
        addLabel={addLabel}
        emptyLabel={emptyLabel}
      />
      {isSaving && (
        <p className={styles.status} role="status">
          Saving…
        </p>
      )}
      {error && <p role="alert">{error}</p>}
    </div>
  )
}

export function CVPresentationDetailPage({ presentationId, onBack, onDeleted }: CVPresentationDetailPageProps) {
  const [loadState, setLoadState] = useState<LoadState>('loading')
  const [presentation, setPresentation] = useState<CVPresentationResponse | null>(null)
  const [profile, setProfile] = useState<ProfessionalProfileResponse | null>(null)
  const [loadError, setLoadError] = useState<string | null>(null)
  const [activeTab, setActiveTab] = useState('edit')
  const [confirmingDelete, setConfirmingDelete] = useState(false)
  const [isDeleting, setIsDeleting] = useState(false)
  const [deleteError, setDeleteError] = useState<string | null>(null)

  const load = async () => {
    try {
      const [presentationData, profileData] = await Promise.all([fetchCVPresentation(presentationId), fetchProfessionalProfile()])
      if (!presentationData || !profileData) {
        setLoadState('not-found')
        return
      }

      setPresentation(presentationData)
      setProfile(profileData)
      setLoadState('ready')
    } catch (caught) {
      setLoadError(describeError(caught, 'Something went wrong loading this CV presentation.'))
      setLoadState('error')
    }
  }

  // Inlined rather than calling load() directly — see study-items pages for why (the
  // set-state-in-effect lint rule treats any call to a state-setting function reference as
  // synchronous, regardless of the await inside it).
  useEffect(() => {
    Promise.all([fetchCVPresentation(presentationId), fetchProfessionalProfile()])
      .then(([presentationData, profileData]) => {
        if (!presentationData || !profileData) {
          setLoadState('not-found')
          return
        }

        setPresentation(presentationData)
        setProfile(profileData)
        setLoadState('ready')
      })
      .catch((caught: unknown) => {
        setLoadError(describeError(caught, 'Something went wrong loading this CV presentation.'))
        setLoadState('error')
      })
  }, [presentationId])

  const handleDelete = async () => {
    setIsDeleting(true)
    setDeleteError(null)
    try {
      await deleteCVPresentation(presentationId)
      onDeleted()
    } catch (caught) {
      setDeleteError(describeError(caught, 'Something went wrong deleting this CV presentation.'))
      setIsDeleting(false)
      setConfirmingDelete(false)
    }
  }

  if (loadState === 'loading') {
    return (
      <p className={styles.status} role="status">
        Loading…
      </p>
    )
  }

  if (loadState === 'not-found') {
    return (
      <div className={styles.page}>
        <p>This CV presentation could not be found.</p>
        <Button onClick={onBack}>Back to CV presentations</Button>
      </div>
    )
  }

  if (loadState === 'error') {
    return (
      <div className={styles.page}>
        <p role="alert">{loadError}</p>
        <Button
          onClick={() => {
            setLoadState('loading')
            void load()
          }}
        >
          Try again
        </Button>
      </div>
    )
  }

  const data = presentation!
  const profileData = profile!

  return (
    <div className={styles.page}>
      <Button variant="ghost" className={styles.back} onClick={onBack}>
        Back to CV presentations
      </Button>

      <header className={styles.header}>
        <h1 className={styles.title}>{data.label}</h1>
        {confirmingDelete ? (
          <span className={styles.confirmRow}>
            <span>Delete this CV presentation permanently?</span>
            <Button variant="danger" onClick={() => void handleDelete()} isLoading={isDeleting}>
              Yes, delete
            </Button>
            <Button variant="ghost" onClick={() => setConfirmingDelete(false)}>
              Cancel
            </Button>
          </span>
        ) : (
          <Button variant="danger" onClick={() => setConfirmingDelete(true)}>
            <Icon name="trash-2" /> Delete
          </Button>
        )}
      </header>

      {deleteError && <p role="alert">{deleteError}</p>}

      <Tabs tabs={[{ key: 'edit', label: 'Edit' }, { key: 'preview', label: 'Preview' }]} activeTab={activeTab} onChange={setActiveTab} aria-label="Edit or preview" />

      <div id={`tabpanel-${activeTab}`} role="tabpanel" aria-labelledby={`tab-${activeTab}`} className={styles.panel}>
        {activeTab === 'edit' && (
          <div className={styles.editPanel}>
            <CVPresentationForm mode="edit" presentation={data} onSaved={() => void load()} onCancel={() => setActiveTab('preview')} />

            <SelectionSection
              title="Experience"
              candidates={profileData.experience}
              selectedIds={data.experienceSelections}
              onSaved={(experienceSelections) => setPresentation({ ...data, experienceSelections })}
              getId={(entry) => entry.id}
              getLabel={(entry) => `${entry.role} — ${entry.company}`}
              addLabel="Add experience entry"
              emptyLabel="No experience entries selected."
              save={replaceExperienceSelections}
              presentationId={presentationId}
            />

            <SelectionSection
              title="Education"
              candidates={profileData.education}
              selectedIds={data.educationSelections}
              onSaved={(educationSelections) => setPresentation({ ...data, educationSelections })}
              getId={(entry) => entry.id}
              getLabel={(entry) => `${entry.degree} — ${entry.institution}`}
              addLabel="Add education entry"
              emptyLabel="No education entries selected."
              save={replaceEducationSelections}
              presentationId={presentationId}
            />

            <SelectionSection
              title="Skills"
              candidates={profileData.skills}
              selectedIds={data.skillSelections}
              onSaved={(skillSelections) => setPresentation({ ...data, skillSelections })}
              getId={(entry) => entry.id}
              getLabel={(entry) => entry.displayName}
              addLabel="Add skill"
              emptyLabel="No skills selected."
              save={replaceSkillSelections}
              presentationId={presentationId}
            />

            <SelectionSection
              title="Languages"
              candidates={profileData.languages}
              selectedIds={data.languageSelections}
              onSaved={(languageSelections) => setPresentation({ ...data, languageSelections })}
              getId={(entry) => entry.id}
              getLabel={(entry) => entry.language}
              addLabel="Add language"
              emptyLabel="No languages selected."
              save={replaceLanguageSelections}
              presentationId={presentationId}
            />

            <SelectionSection
              title="Certifications"
              candidates={profileData.certifications}
              selectedIds={data.certificationSelections}
              onSaved={(certificationSelections) => setPresentation({ ...data, certificationSelections })}
              getId={(entry) => entry.id}
              getLabel={(entry) => entry.name}
              addLabel="Add certification"
              emptyLabel="No certifications selected."
              save={replaceCertificationSelections}
              presentationId={presentationId}
            />

            <SelectionSection
              title="Projects"
              candidates={profileData.projects}
              selectedIds={data.projectSelections}
              onSaved={(projectSelections) => setPresentation({ ...data, projectSelections })}
              getId={(entry) => entry.id}
              getLabel={(entry) => entry.name}
              addLabel="Add project"
              emptyLabel="No projects selected."
              save={replaceProjectSelections}
              presentationId={presentationId}
            />

            <SelectionSection
              title="Links"
              candidates={profileData.profileLinks}
              selectedIds={data.profileLinkSelections}
              onSaved={(profileLinkSelections) => setPresentation({ ...data, profileLinkSelections })}
              getId={(entry) => entry.id}
              getLabel={(entry) => entry.label ?? entry.kind}
              addLabel="Add link"
              emptyLabel="No links selected."
              save={replaceProfileLinkSelections}
              presentationId={presentationId}
            />
          </div>
        )}

        {activeTab === 'preview' && <CVPreview profile={profileData} presentation={data} />}
      </div>
    </div>
  )
}
