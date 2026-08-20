import { useEffect, useRef, useState } from 'react'
import { Button } from '../../design-system/components/Button'
import { Field } from '../../design-system/components/Field'
import { Icon } from '../../design-system/Icon'
import { SectionNav, type SectionNavItem } from '../../design-system/components/SectionNav'
import inputStyles from '../../design-system/components/Input.module.css'
import { createProfessionalProfile, fetchProfessionalProfile, type ContactInfoDto, type ProfessionalProfileResponse } from './api'
import { ContactInfoFields } from './fields/ContactInfoFields'
import { AboutYouSection } from './sections/AboutYouSection'
import { CertificationsSection } from './sections/CertificationsSection'
import { EducationSection } from './sections/EducationSection'
import { ExperienceSection, type FocusRequest } from './sections/ExperienceSection'
import { LanguagesSection } from './sections/LanguagesSection'
import { LinksSection } from './sections/LinksSection'
import { ProjectsSection } from './sections/ProjectsSection'
import { SkillsSection } from './sections/SkillsSection'
import { ProfileCoverage } from './ProfileCoverage'
import { ProfilePreview, type HighlightedAchievement } from './ProfilePreview'
import layout from './FormLayout.module.css'
import styles from './ProfessionalProfilePage.module.css'

type LoadState = 'loading' | 'not-found' | 'ready' | 'error'

const EMPTY_CONTACT_INFO: ContactInfoDto = { name: '', email: '', phone: null, address: null, photoStorageKey: null }

function describeError(caught: unknown, fallback: string): string {
  return caught instanceof Error ? caught.message : fallback
}

function buildNavItems(profile: ProfessionalProfileResponse): SectionNavItem[] {
  const missingImpact = profile.experience.filter((entry) => entry.achievements.length === 0).length
  const missingLinks = profile.certifications.filter((entry) => !entry.url).length

  return [
    { key: 'about-you', label: 'About you' },
    { key: 'experience', label: 'Experience', count: profile.experience.length, severity: missingImpact > 0 ? 'caution' : undefined },
    { key: 'education', label: 'Education', count: profile.education.length },
    { key: 'skills', label: 'Skills', count: profile.skills.length },
    { key: 'languages', label: 'Languages', count: profile.languages.length },
    { key: 'certifications', label: 'Certifications', count: profile.certifications.length, severity: missingLinks > 0 ? 'caution' : undefined },
    { key: 'projects', label: 'Projects', count: profile.projects.length },
    { key: 'links', label: 'Links', count: profile.profileLinks.length },
  ]
}

export function ProfessionalProfilePage() {
  const [loadState, setLoadState] = useState<LoadState>('loading')
  const [profile, setProfile] = useState<ProfessionalProfileResponse | null>(null)
  const [loadError, setLoadError] = useState<string | null>(null)
  const [createContactInfo, setCreateContactInfo] = useState<ContactInfoDto>(EMPTY_CONTACT_INFO)
  const [createSummary, setCreateSummary] = useState('')
  const [createError, setCreateError] = useState<string | null>(null)
  const [isCreating, setIsCreating] = useState(false)
  const [showFrame, setShowFrame] = useState(true)
  const [focusRequest, setFocusRequest] = useState<FocusRequest | null>(null)
  const [highlighted, setHighlighted] = useState<HighlightedAchievement>(null)
  const focusToken = useRef(0)
  const previewDialogRef = useRef<HTMLDialogElement>(null)

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

  const focusExperience = (id: string) => {
    focusToken.current += 1
    setFocusRequest({ id, token: focusToken.current })
  }

  return (
    <>
      <header className={styles.pageHeader}>
        <h1 className={styles.title}>Professional profile</h1>
        <div className={styles.pageHeaderActions}>
          {/* Below 1280px the preview column (ProfilePreview) is no longer stacked inline — this is
              the one control that reaches it instead, per docs/design/design-system/page-patterns.md
              "Responsive baseline" ("exactly one preview control is visible at any width"). */}
          <span className={styles.previewTrigger}>
            <Button type="button" variant="secondary" onClick={() => previewDialogRef.current?.showModal()}>
              Preview
            </Button>
          </span>
          {/* Disabled and future-only — hidden below the width where it and "Preview" no longer
              both fit next to the page title without wrapping or overflowing. */}
          <span className={styles.importTrigger}>
            <Button type="button" variant="secondary" disabled title="Coming later">
              Import from LinkedIn
            </Button>
          </span>
        </div>
      </header>

      <SectionNav items={buildNavItems(data)} aria-label="Profile sections" />

      <div className={styles.split}>
        <div className={styles.column}>
          {showFrame && (
            <div className={styles.frame}>
              <div className={styles.frameIcon}>
                <Icon name="database" />
              </div>
              <p>
                <strong>This is your master profile — not a CV.</strong> Each CV you create selects, orders and rewrites from this record.
              </p>
              <button type="button" className={styles.frameClose} onClick={() => setShowFrame(false)} aria-label="Dismiss">
                <Icon name="x" />
              </button>
            </div>
          )}

          <AboutYouSection
            contactInfo={data.contactInfo}
            summaryMarkdown={data.summaryMarkdown}
            onChange={(contactInfo, summaryMarkdown) => setProfile((current) => (current ? { ...current, contactInfo, summaryMarkdown } : current))}
          />

          <ExperienceSection
            experience={data.experience}
            skills={data.skills}
            onChange={(experience) => setProfile((current) => (current ? { ...current, experience } : current))}
            focusRequest={focusRequest}
            onHighlightAchievement={(experienceId, index) => setHighlighted(index === null ? null : { experienceId, index })}
          />

          <EducationSection education={data.education} onChange={(education) => setProfile((current) => (current ? { ...current, education } : current))} />

          <SkillsSection skills={data.skills} onChange={(skills) => setProfile((current) => (current ? { ...current, skills } : current))} />

          <LanguagesSection languages={data.languages} onChange={(languages) => setProfile((current) => (current ? { ...current, languages } : current))} />

          <CertificationsSection
            certifications={data.certifications}
            onChange={(certifications) => setProfile((current) => (current ? { ...current, certifications } : current))}
          />

          <ProjectsSection
            projects={data.projects}
            skills={data.skills}
            onChange={(projects) => setProfile((current) => (current ? { ...current, projects } : current))}
          />

          <LinksSection profileLinks={data.profileLinks} onChange={(profileLinks) => setProfile((current) => (current ? { ...current, profileLinks } : current))} />
        </div>

        <aside className={styles.aside} aria-label="Profile coverage and preview">
          <ProfileCoverage profile={data} />
          <div className={styles.previewInline}>
            <ProfilePreview profile={data} highlighted={highlighted} onFocusExperience={focusExperience} />
          </div>
        </aside>
      </div>

      {/* Reuses ProfilePreview as-is inside the browser's native <dialog> — no generic overlay
          framework, just the platform primitive: showModal() gets focus trapping, Escape-to-close,
          and a ::backdrop for free. Always present in the DOM (a closed <dialog> paints nothing and
          is excluded from the accessibility tree), so there's nothing to conditionally mount. */}
      <dialog
        ref={previewDialogRef}
        className={styles.previewDialog}
        aria-label="Profile preview"
        onClick={(event) => {
          if (event.target === previewDialogRef.current) previewDialogRef.current?.close()
        }}
      >
        <button type="button" className={styles.previewDialogClose} onClick={() => previewDialogRef.current?.close()} aria-label="Close preview">
          <Icon name="x" />
        </button>
        <ProfilePreview
          profile={data}
          highlighted={highlighted}
          onFocusExperience={(id) => {
            previewDialogRef.current?.close()
            focusExperience(id)
          }}
        />
      </dialog>
    </>
  )
}
