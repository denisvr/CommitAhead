import { useEffect, useState } from 'react'
import { Button } from '../../design-system/components/Button'
import { Field } from '../../design-system/components/Field'
import { Tabs } from '../../design-system/components/Tabs'
import inputStyles from '../../design-system/components/Input.module.css'
import {
  createProfessionalProfile,
  fetchProfessionalProfile,
  replaceCertifications,
  replaceEducation,
  replaceExperience,
  replaceLanguages,
  replaceProfileLinks,
  replaceProjects,
  replaceSkills,
  updateProfessionalProfile,
  type CertificationEntryDto,
  type ContactInfoDto,
  type EducationEntryDto,
  type ExperienceEntryDto,
  type LanguageEntryDto,
  type ProfessionalProfileResponse,
  type ProfileLinkDto,
  type ProjectEntryDto,
  type SkillDto,
} from './api'
import { CollectionSection } from './CollectionSection'
import { CertificationEntryFields } from './fields/CertificationEntryFields'
import { ContactInfoFields } from './fields/ContactInfoFields'
import { EducationEntryFields } from './fields/EducationEntryFields'
import { ExperienceEntryFields } from './fields/ExperienceEntryFields'
import { LanguageEntryFields } from './fields/LanguageEntryFields'
import { ProfileLinkFields } from './fields/ProfileLinkFields'
import { ProjectEntryFields } from './fields/ProjectEntryFields'
import { SkillFields } from './fields/SkillFields'
import layout from './FormLayout.module.css'
import styles from './ProfessionalProfilePage.module.css'

type LoadState = 'loading' | 'not-found' | 'ready' | 'error'

const EMPTY_CONTACT_INFO: ContactInfoDto = { name: '', email: '', phone: null, address: null, photoStorageKey: null }

const SECTION_TABS = [
  { key: 'contact', label: 'Contact & summary' },
  { key: 'experience', label: 'Experience' },
  { key: 'education', label: 'Education' },
  { key: 'skills', label: 'Skills' },
  { key: 'languages', label: 'Languages' },
  { key: 'certifications', label: 'Certifications' },
  { key: 'projects', label: 'Projects' },
  { key: 'links', label: 'Links' },
]

function describeError(caught: unknown, fallback: string): string {
  return caught instanceof Error ? caught.message : fallback
}

function ContactSummarySection({ profile, onSaved }: { profile: ProfessionalProfileResponse; onSaved: (contactInfo: ContactInfoDto, summaryMarkdown: string) => void }) {
  const [contactInfo, setContactInfo] = useState(profile.contactInfo)
  const [summaryMarkdown, setSummaryMarkdown] = useState(profile.summaryMarkdown)
  const [error, setError] = useState<string | null>(null)
  const [isSaving, setIsSaving] = useState(false)

  const handleSave = async () => {
    setIsSaving(true)
    setError(null)
    try {
      await updateProfessionalProfile(contactInfo, summaryMarkdown)
      onSaved(contactInfo, summaryMarkdown)
    } catch (caught) {
      setError(describeError(caught, 'Something went wrong saving your contact info and summary.'))
    } finally {
      setIsSaving(false)
    }
  }

  return (
    <div className={layout.stack}>
      <ContactInfoFields value={contactInfo} onChange={setContactInfo} />
      <Field label="Summary">
        {(fieldProps) => <textarea {...fieldProps} className={inputStyles.input} value={summaryMarkdown} onChange={(event) => setSummaryMarkdown(event.target.value)} />}
      </Field>
      {error && <p role="alert">{error}</p>}
      <Button type="button" variant="primary" onClick={handleSave} isLoading={isSaving}>
        Save
      </Button>
    </div>
  )
}

export function ProfessionalProfilePage() {
  const [loadState, setLoadState] = useState<LoadState>('loading')
  const [profile, setProfile] = useState<ProfessionalProfileResponse | null>(null)
  const [loadError, setLoadError] = useState<string | null>(null)
  const [activeTab, setActiveTab] = useState('contact')
  const [createContactInfo, setCreateContactInfo] = useState<ContactInfoDto>(EMPTY_CONTACT_INFO)
  const [createSummary, setCreateSummary] = useState('')
  const [createError, setCreateError] = useState<string | null>(null)
  const [isCreating, setIsCreating] = useState(false)

  const load = async () => {
    try {
      const data = await fetchProfessionalProfile()
      if (!data) {
        setLoadState('not-found')
        return
      }

      setProfile(data)
      setLoadState('ready')
    } catch (caught) {
      setLoadError(describeError(caught, 'Something went wrong loading your professional profile.'))
      setLoadState('error')
    }
  }

  // Inlined rather than calling load() directly — the set-state-in-effect lint rule treats any
  // call to a state-setting function reference as synchronous, regardless of the await inside it.
  useEffect(() => {
    fetchProfessionalProfile()
      .then((data) => {
        if (!data) {
          setLoadState('not-found')
          return
        }

        setProfile(data)
        setLoadState('ready')
      })
      .catch((caught: unknown) => {
        setLoadError(describeError(caught, 'Something went wrong loading your professional profile.'))
        setLoadState('error')
      })
  }, [])

  const handleCreate = async () => {
    setIsCreating(true)
    setCreateError(null)
    try {
      const created = await createProfessionalProfile(createContactInfo, createSummary)
      if (!created) {
        setCreateError('A professional profile already exists for your account.')
        return
      }

      setLoadState('loading')
      await load()
    } catch (caught) {
      setCreateError(describeError(caught, 'Something went wrong creating your professional profile.'))
    } finally {
      setIsCreating(false)
    }
  }

  if (loadState === 'loading') {
    return (
      <p className={styles.status} role="status">
        Loading…
      </p>
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

  if (loadState === 'not-found') {
    return (
      <div className={[styles.page, layout.stack].join(' ')}>
        <h1 className={styles.title}>Create your professional profile</h1>
        <p className={styles.hint}>This is the canonical source for your career data — you can curate it into CV presentations afterwards.</p>
        <ContactInfoFields value={createContactInfo} onChange={setCreateContactInfo} />
        <Field label="Summary">
          {(fieldProps) => <textarea {...fieldProps} required className={inputStyles.input} value={createSummary} onChange={(event) => setCreateSummary(event.target.value)} />}
        </Field>
        {createError && <p role="alert">{createError}</p>}
        <Button type="button" variant="primary" onClick={handleCreate} isLoading={isCreating}>
          Create profile
        </Button>
      </div>
    )
  }

  const data = profile!

  return (
    <div className={styles.page}>
      <h1 className={styles.title}>Professional profile</h1>

      <Tabs tabs={SECTION_TABS} activeTab={activeTab} onChange={setActiveTab} aria-label="Profile sections" />

      <div id={`tabpanel-${activeTab}`} role="tabpanel" aria-labelledby={`tab-${activeTab}`} className={styles.panel}>
        {activeTab === 'contact' && (
          <ContactSummarySection
            profile={data}
            onSaved={(contactInfo, summaryMarkdown) => setProfile({ ...data, contactInfo, summaryMarkdown })}
          />
        )}

        {activeTab === 'experience' && (
          <CollectionSection<ExperienceEntryDto>
            entries={data.experience}
            onSaved={(experience) => setProfile({ ...data, experience })}
            createEntry={() => ({
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
            })}
            getKey={(entry) => entry.id}
            addLabel="Add role"
            emptyLabel="No experience entries yet."
            renderEntry={(entry, onChange, onRemove) => <ExperienceEntryFields value={entry} onChange={onChange} onRemove={onRemove} skills={data.skills} />}
            save={replaceExperience}
          />
        )}

        {activeTab === 'education' && (
          <CollectionSection<EducationEntryDto>
            entries={data.education}
            onSaved={(education) => setProfile({ ...data, education })}
            createEntry={() => ({ id: crypto.randomUUID(), institution: '', degree: '', field: null, startDate: null, endDate: null, location: null, detailsMarkdown: null })}
            getKey={(entry) => entry.id}
            addLabel="Add education"
            emptyLabel="No education entries yet."
            renderEntry={(entry, onChange, onRemove) => <EducationEntryFields value={entry} onChange={onChange} onRemove={onRemove} />}
            save={replaceEducation}
          />
        )}

        {activeTab === 'skills' && (
          <CollectionSection<SkillDto>
            entries={data.skills}
            onSaved={(skills) => setProfile({ ...data, skills })}
            createEntry={() => ({ id: crypto.randomUUID(), displayName: '', normalizedKey: '', category: 'Other', proficiency: null })}
            getKey={(entry) => entry.id}
            addLabel="Add skill"
            emptyLabel="No skills yet."
            renderEntry={(entry, onChange, onRemove) => <SkillFields value={entry} onChange={onChange} onRemove={onRemove} />}
            save={replaceSkills}
          />
        )}

        {activeTab === 'languages' && (
          <CollectionSection<LanguageEntryDto>
            entries={data.languages}
            onSaved={(languages) => setProfile({ ...data, languages })}
            createEntry={() => ({ id: crypto.randomUUID(), language: '', proficiency: 'B1', certification: null })}
            getKey={(entry) => entry.id}
            addLabel="Add language"
            emptyLabel="No languages yet."
            renderEntry={(entry, onChange, onRemove) => <LanguageEntryFields value={entry} onChange={onChange} onRemove={onRemove} />}
            save={replaceLanguages}
          />
        )}

        {activeTab === 'certifications' && (
          <CollectionSection<CertificationEntryDto>
            entries={data.certifications}
            onSaved={(certifications) => setProfile({ ...data, certifications })}
            createEntry={() => ({ id: crypto.randomUUID(), name: '', issuingOrganisation: '', issuedAt: null, expiresAt: null, credentialId: null, url: null })}
            getKey={(entry) => entry.id}
            addLabel="Add certification"
            emptyLabel="No certifications yet."
            renderEntry={(entry, onChange, onRemove) => <CertificationEntryFields value={entry} onChange={onChange} onRemove={onRemove} />}
            save={replaceCertifications}
          />
        )}

        {activeTab === 'projects' && (
          <CollectionSection<ProjectEntryDto>
            entries={data.projects}
            onSaved={(projects) => setProfile({ ...data, projects })}
            createEntry={() => ({ id: crypto.randomUUID(), name: '', role: null, startDate: null, endDate: null, descriptionMarkdown: '', url: null, skillIds: [] })}
            getKey={(entry) => entry.id}
            addLabel="Add project"
            emptyLabel="No projects yet."
            renderEntry={(entry, onChange, onRemove) => <ProjectEntryFields value={entry} onChange={onChange} onRemove={onRemove} skills={data.skills} />}
            save={replaceProjects}
          />
        )}

        {activeTab === 'links' && (
          <CollectionSection<ProfileLinkDto>
            entries={data.profileLinks}
            onSaved={(profileLinks) => setProfile({ ...data, profileLinks })}
            createEntry={() => ({ id: crypto.randomUUID(), kind: 'Other', label: null, url: '' })}
            getKey={(entry) => entry.id}
            addLabel="Add link"
            emptyLabel="No links yet."
            renderEntry={(entry, onChange, onRemove) => <ProfileLinkFields value={entry} onChange={onChange} onRemove={onRemove} />}
            save={replaceProfileLinks}
          />
        )}
      </div>
    </div>
  )
}
