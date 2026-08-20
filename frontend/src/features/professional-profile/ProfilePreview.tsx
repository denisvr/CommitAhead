import { RestrictedMarkdown } from '../../design-system/components/RestrictedMarkdown'
import type { ProfessionalProfileResponse } from './api'
import { compareStartDateDesc, formatDateRange, formatMonthYear } from './formatDate'
import styles from './ProfilePreview.module.css'

export type HighlightedAchievement = { experienceId: string; index: number } | null

type ProfilePreviewProps = {
  profile: ProfessionalProfileResponse
  highlighted: HighlightedAchievement
  onFocusExperience: (id: string) => void
}

const SHOWN_ACHIEVEMENTS = 3

// A picture of the profile through a template, not a saved CV — a real CVPresentation curates a
// subset of these same entries (see CVPreview) and can be reused across markets; this always
// shows everything so the "+N more" note below can teach the Profile = truth / CV = strategy
// distinction. Deliberately no template selector: only one export template exists today
// (ExportCVPresentationUseCase.SupportedTemplateKey), and CLAUDE.md is explicit that later-phase
// behaviour is never implemented from a mock — a dropdown offering templates that do not exist
// would be exactly that.
export function ProfilePreview({ profile, highlighted, onFocusExperience }: ProfilePreviewProps) {
  const skillNames = (skillIds: string[]) => {
    const byId = new Map(profile.skills.map((skill) => [skill.id, skill.displayName]))
    return skillIds.map((id) => byId.get(id)).filter((name): name is string => Boolean(name))
  }

  const experience = [...profile.experience].sort(compareStartDateDesc)
  const contactLine = [profile.contactInfo.email, profile.contactInfo.phone, profile.contactInfo.address].filter(Boolean)

  return (
    <div className={styles.pvWrap}>
      <div className={styles.pvBar}>
        <h3>Profile preview</h3>
      </div>
      <p className={styles.pvCap}>
        Your profile through a template — <b>not a saved CV</b>, but the same content an export would print today. Select anything below to jump to it.
      </p>

      <div className={styles.paper}>
        <div className={styles.paperScroll}>
          <h1 className={styles.name}>{profile.contactInfo.name || 'Your name'}</h1>
          {contactLine.length > 0 && (
            <p className={styles.contact}>
              {contactLine.map((item, index) => (
                <span key={index}>{item}</span>
              ))}
            </p>
          )}

          {profile.summaryMarkdown.trim() !== '' && (
            <>
              <p className={styles.h2}>Profile</p>
              <RestrictedMarkdown className={styles.summary}>{profile.summaryMarkdown}</RestrictedMarkdown>
            </>
          )}

          {experience.length > 0 && (
            <>
              <p className={styles.h2}>Work experience</p>
              {experience.map((entry) => {
                const shown = entry.achievements.slice(0, SHOWN_ACHIEVEMENTS)
                const remaining = entry.achievements.length - shown.length
                return (
                  <button key={entry.id} type="button" className={styles.job} onClick={() => onFocusExperience(entry.id)}>
                    <span className={styles.jobHead}>
                      <span className={styles.jobTitle}>{entry.role || 'Untitled role'}</span>
                      <span className={styles.jobDate}>{formatDateRange(entry.startDate, entry.endDate)}</span>
                    </span>
                    <span className={styles.jobOrg}>{[entry.company, entry.client].filter(Boolean).join(' — client: ')}</span>
                    {shown.length === 0 ? (
                      <span className={styles.flag}>Nothing recorded — prints as a bare heading.</span>
                    ) : (
                      <ul className={styles.achievementList}>
                        {shown.map((achievement, index) => (
                          <li key={index} className={highlighted?.experienceId === entry.id && highlighted.index === index ? styles.highlighted : undefined}>
                            {achievement}
                          </li>
                        ))}
                      </ul>
                    )}
                    {remaining > 0 && (
                      <span className={[styles.more, highlighted?.experienceId === entry.id && highlighted.index >= SHOWN_ACHIEVEMENTS ? styles.highlighted : ''].join(' ').trim()}>
                        + {remaining} more achievement{remaining === 1 ? '' : 's'} in your profile — this preview shows {SHOWN_ACHIEVEMENTS}.
                      </span>
                    )}
                    {skillNames(entry.skillIds).length > 0 && <span className={styles.tags}>{skillNames(entry.skillIds).join(' · ')}</span>}
                  </button>
                )
              })}
            </>
          )}

          {profile.education.length > 0 && (
            <>
              <p className={styles.h2}>Education and training</p>
              {profile.education.map((entry) => (
                <div key={entry.id} className={styles.plainEntry}>
                  <span className={styles.jobHead}>
                    <span className={styles.jobTitle}>{entry.degree || 'Untitled qualification'}</span>
                    {entry.startDate && <span className={styles.jobDate}>{formatDateRange(entry.startDate, entry.endDate)}</span>}
                  </span>
                  <span className={styles.jobOrg}>{entry.institution}</span>
                </div>
              ))}
            </>
          )}

          {profile.skills.length > 0 && (
            <>
              <p className={styles.h2}>Skills</p>
              <p className={styles.tags}>{profile.skills.map((skill) => skill.displayName).join(' · ')}</p>
            </>
          )}

          {profile.languages.length > 0 && (
            <>
              <p className={styles.h2}>Languages</p>
              {profile.languages.map((entry) => (
                <p key={entry.id} className={styles.lineRow}>
                  <span>{entry.language}</span>
                  <span>{entry.proficiency}</span>
                </p>
              ))}
            </>
          )}

          {profile.certifications.length > 0 && (
            <>
              <p className={styles.h2}>Certifications</p>
              {profile.certifications.map((entry) => (
                <p key={entry.id} className={styles.lineRow}>
                  <span>{entry.name}</span>
                  <span>
                    {entry.issuingOrganisation}
                    {entry.issuedAt ? `, ${formatMonthYear(entry.issuedAt)}` : ''}
                  </span>
                </p>
              ))}
            </>
          )}
        </div>
      </div>
    </div>
  )
}
