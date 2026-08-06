import type { ReactNode } from 'react'
import { restrictedUrl } from '../../design-system/components/restrictedUrlTransform'
import { RestrictedMarkdown } from '../../design-system/components/RestrictedMarkdown'
import type { ProfessionalProfileResponse } from '../professional-profile/api'
import { formatYearMonth } from './formatYearMonth'
import type { CVPresentationResponse } from './api'
import styles from './CVPreview.module.css'

type CVPreviewProps = {
  profile: ProfessionalProfileResponse
  presentation: CVPresentationResponse
}

function SafeLink({ url, children }: { url: string; children: ReactNode }) {
  const safeUrl = restrictedUrl(url)
  if (!safeUrl) {
    return <>{children}</>
  }

  return (
    <a href={safeUrl} target="_blank" rel="noopener noreferrer nofollow">
      {children}
    </a>
  )
}

// Read-only assembled view built directly from already-fetched data — never duplicates canonical
// content server-side (ADR-0012), and is not wired to any export/template engine, which doesn't
// exist yet (Phase 5). Bounded by a rule, not a floating card, per page-patterns.md.
export function CVPreview({ profile, presentation }: CVPreviewProps) {
  const locale = presentation.locale
  const bySkillId = new Map(profile.skills.map((skill) => [skill.id, skill.displayName]))
  const skillNames = (skillIds: string[]) => skillIds.map((id) => bySkillId.get(id)).filter((name): name is string => Boolean(name))

  const experience = presentation.experienceSelections.map((id) => profile.experience.find((entry) => entry.id === id)).filter((entry) => entry !== undefined)
  const education = presentation.educationSelections.map((id) => profile.education.find((entry) => entry.id === id)).filter((entry) => entry !== undefined)
  const skills = presentation.skillSelections.map((id) => profile.skills.find((entry) => entry.id === id)).filter((entry) => entry !== undefined)
  const languages = presentation.languageSelections.map((id) => profile.languages.find((entry) => entry.id === id)).filter((entry) => entry !== undefined)
  const certifications = presentation.certificationSelections.map((id) => profile.certifications.find((entry) => entry.id === id)).filter((entry) => entry !== undefined)
  const projects = presentation.projectSelections.map((id) => profile.projects.find((entry) => entry.id === id)).filter((entry) => entry !== undefined)
  const profileLinks = presentation.profileLinkSelections.map((id) => profile.profileLinks.find((entry) => entry.id === id)).filter((entry) => entry !== undefined)

  return (
    <div className={styles.preview}>
      <header className={styles.header}>
        <h2 className={styles.name}>{profile.contactInfo.name}</h2>
        <p className={styles.contactLine}>
          {presentation.includeEmail && <span>{profile.contactInfo.email}</span>}
          {presentation.includePhone && profile.contactInfo.phone && <span>{profile.contactInfo.phone}</span>}
          {presentation.includeAddress && profile.contactInfo.address && <span>{profile.contactInfo.address}</span>}
        </p>
      </header>

      <RestrictedMarkdown className={styles.summary}>{presentation.summaryOverrideMarkdown ?? profile.summaryMarkdown}</RestrictedMarkdown>

      {profileLinks.length > 0 && (
        <ul className={styles.linkList}>
          {profileLinks.map((link) => (
            <li key={link.id}>
              <SafeLink url={link.url}>{link.label ?? link.kind}</SafeLink>
            </li>
          ))}
        </ul>
      )}

      {experience.length > 0 && (
        <section className={styles.section} aria-label="Experience">
          <h3 className={styles.sectionTitle}>Experience</h3>
          {experience.map((entry) => (
            <article key={entry.id} className={styles.entry}>
              <p className={styles.entryHeading}>
                {entry.role} — {entry.company}
              </p>
              <p className={styles.entryMeta}>
                {formatYearMonth(entry.startDate, locale)} – {entry.endDate ? formatYearMonth(entry.endDate, locale) : 'Present'}
                {entry.location ? ` · ${entry.location}` : ''}
              </p>
              <RestrictedMarkdown>{entry.summaryMarkdown}</RestrictedMarkdown>
              {entry.achievements.length > 0 && (
                <ul>
                  {entry.achievements.map((achievement) => (
                    <li key={achievement}>{achievement}</li>
                  ))}
                </ul>
              )}
              {skillNames(entry.skillIds).length > 0 && <p className={styles.entryMeta}>{skillNames(entry.skillIds).join(', ')}</p>}
            </article>
          ))}
        </section>
      )}

      {education.length > 0 && (
        <section className={styles.section} aria-label="Education">
          <h3 className={styles.sectionTitle}>Education</h3>
          {education.map((entry) => (
            <article key={entry.id} className={styles.entry}>
              <p className={styles.entryHeading}>
                {entry.degree} — {entry.institution}
              </p>
              <p className={styles.entryMeta}>
                {entry.startDate ? formatYearMonth(entry.startDate, locale) : ''}
                {entry.endDate ? ` – ${formatYearMonth(entry.endDate, locale)}` : ''}
                {entry.location ? ` · ${entry.location}` : ''}
              </p>
              {entry.detailsMarkdown && <RestrictedMarkdown>{entry.detailsMarkdown}</RestrictedMarkdown>}
            </article>
          ))}
        </section>
      )}

      {skills.length > 0 && (
        <section className={styles.section} aria-label="Skills">
          <h3 className={styles.sectionTitle}>Skills</h3>
          <p>{skills.map((skill) => skill.displayName).join(', ')}</p>
        </section>
      )}

      {languages.length > 0 && (
        <section className={styles.section} aria-label="Languages">
          <h3 className={styles.sectionTitle}>Languages</h3>
          <p>{languages.map((language) => `${language.language} (${language.proficiency})`).join(', ')}</p>
        </section>
      )}

      {certifications.length > 0 && (
        <section className={styles.section} aria-label="Certifications">
          <h3 className={styles.sectionTitle}>Certifications</h3>
          {certifications.map((entry) => (
            <article key={entry.id} className={styles.entry}>
              <p className={styles.entryHeading}>
                {entry.name} — {entry.issuingOrganisation}
              </p>
              <p className={styles.entryMeta}>
                {entry.issuedAt ? formatYearMonth(entry.issuedAt, locale) : ''}
                {entry.expiresAt ? ` – ${formatYearMonth(entry.expiresAt, locale)}` : ''}
              </p>
            </article>
          ))}
        </section>
      )}

      {projects.length > 0 && (
        <section className={styles.section} aria-label="Projects">
          <h3 className={styles.sectionTitle}>Projects</h3>
          {projects.map((entry) => (
            <article key={entry.id} className={styles.entry}>
              <p className={styles.entryHeading}>{entry.role ? `${entry.name} — ${entry.role}` : entry.name}</p>
              <p className={styles.entryMeta}>
                {entry.startDate ? formatYearMonth(entry.startDate, locale) : ''}
                {entry.endDate ? ` – ${formatYearMonth(entry.endDate, locale)}` : ''}
              </p>
              <RestrictedMarkdown>{entry.descriptionMarkdown}</RestrictedMarkdown>
              {entry.url && (
                <p className={styles.entryMeta}>
                  <SafeLink url={entry.url}>{entry.url}</SafeLink>
                </p>
              )}
              {skillNames(entry.skillIds).length > 0 && <p className={styles.entryMeta}>{skillNames(entry.skillIds).join(', ')}</p>}
            </article>
          ))}
        </section>
      )}
    </div>
  )
}
