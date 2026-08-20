import type { ProfessionalProfileResponse } from './api'
import { scrollToSection } from './scrollToSection'
import styles from './ProfileCoverage.module.css'

type Check = { id: string; tone: 'critical' | 'caution'; text: string }

// Provisional, on purpose: this counts how many of six broad signals are present, weighted
// equally. It is deliberately NOT "career-knowledge coverage" — that measure needs employment
// duration represented, data quality, and per-entry richness, and the product decision so far is
// to not build that formula yet (see ADR-0024 / docs/design/design-system notes). Replace this
// function, not its caller, when that formula exists. The six signals below are also spelled out
// verbatim in the caption rendered next to the percentage, so the number never implies more than
// it measures.
const SIGNAL_COUNT = 6

function computeCoverage(profile: ProfessionalProfileResponse): number {
  const signals = [
    profile.contactInfo.name.trim() !== '' && profile.contactInfo.email.trim() !== '' && profile.summaryMarkdown.trim() !== '',
    profile.experience.length > 0,
    profile.experience.some((entry) => entry.achievements.length > 0),
    profile.education.length > 0,
    profile.skills.length > 0,
    profile.languages.length > 0,
  ]
  return Math.round((signals.filter(Boolean).length / signals.length) * 100)
}

// Only real, derivable facts — never a fabricated "this looks like the wrong content" check,
// which would need judgement this code cannot make.
function computeChecks(profile: ProfessionalProfileResponse): Check[] {
  const checks: Check[] = []

  const noImpact = profile.experience.filter((entry) => entry.achievements.length === 0).length
  if (noImpact > 0) {
    checks.push({
      id: 'experience',
      tone: 'caution',
      text: `${noImpact} position${noImpact === 1 ? '' : 's'} have nothing recorded — no CV can draw on ${noImpact === 1 ? 'it' : 'them'}.`,
    })
  }

  const missingLinks = profile.certifications.filter((entry) => !entry.url).length
  if (missingLinks > 0) {
    checks.push({ id: 'certifications', tone: 'caution', text: `${missingLinks} certification${missingLinks === 1 ? '' : 's'} ${missingLinks === 1 ? 'has' : 'have'} no verification link.` })
  }

  return checks
}

export function ProfileCoverage({ profile }: { profile: ProfessionalProfileResponse }) {
  const coverage = computeCoverage(profile)
  const checks = computeChecks(profile)

  return (
    <div className={styles.panel}>
      <div className={styles.cov}>
        <h3>Basic signals</h3>
        <span className={styles.spacer} />
        <b>{coverage}%</b>
      </div>
      {/* A native <progress>, not a width-styled div — no inline style attribute is allowed
          under the production CSP (ADR-0016), and this is the semantic, accessible element for
          exactly this value anyway. */}
      <progress className={styles.bar} value={coverage} max={100} aria-label={`Basic signals present: ${coverage}%`} />
      <p className={styles.exp}>
        {SIGNAL_COUNT} basic signals, equally weighted: name, email and summary filled in; at least one experience; an achievement recorded on one of them; education; a skill; a language. Not a
        score of how complete your career history is or how good a CV would be.
      </p>

      {checks.length > 0 && (
        <details className={styles.attn}>
          <summary className={styles.summary}>
            {checks.length} need{checks.length === 1 ? 's' : ''} your attention
            <span className={styles.spacer} />
            <span className={styles.chev}>▼</span>
          </summary>
          <ul className={styles.checks}>
            {checks.map((check) => (
              <li key={check.id}>
                <button type="button" className={styles.checkButton} onClick={() => scrollToSection(check.id)}>
                  <span className={[styles.mark, styles[check.tone]].join(' ')}>{check.tone === 'critical' ? '✕' : '!'}</span>
                  <span>{check.text}</span>
                </button>
              </li>
            ))}
          </ul>
          <p className={styles.checksFoot}>A missing optional section is never flagged — bad data inside one still is. Job match is measured per CV, not here.</p>
        </details>
      )}
    </div>
  )
}
