import { Button } from '../../design-system/components/Button'
import { Card } from '../../design-system/components/Card'
import styles from './HomePage.module.css'

type HomePageProps = {
  onOpenProfile: () => void
  onOpenPresentations: () => void
  onCreateCV: () => void
}

// The landing view reached via the brand mark and the Sidebar's "Home" item — a mock-free
// shortcut hub, not a dashboard with invented metrics (no fabricated completeness scores or
// counts; those would need real aggregation this slice doesn't build yet).
export function HomePage({ onOpenProfile, onOpenPresentations, onCreateCV }: HomePageProps) {
  return (
    <div className={styles.page}>
      <div className={styles.hero}>
        <h1 className={styles.title}>Welcome back</h1>
        <p className={styles.subtitle}>CommitAhead keeps your career story in one place, and lets you tailor it into a CV for any role or market.</p>
      </div>

      <Card
        icon="user-round"
        heading="Professional profile"
        lead="Your career record — experience, education, skills, languages, certifications, projects and links."
        actions={
          <Button variant="primary" onClick={onOpenProfile}>
            Open profile
          </Button>
        }
      >
        <p className={styles.cardNote}>This is the canonical source. Nothing here targets a specific job — that&apos;s what a CV presentation is for.</p>
      </Card>

      <Card
        icon="file-text"
        heading="CV presentations"
        lead="Select, order and rewrite entries from your profile into a tailored CV for a specific market or role."
        actions={
          <>
            <Button variant="secondary" onClick={onOpenPresentations}>
              View all
            </Button>
            <Button variant="primary" onClick={onCreateCV}>
              Create a CV →
            </Button>
          </>
        }
      >
        <p className={styles.cardNote}>A CV presentation never duplicates or edits your profile directly — it only selects from it.</p>
      </Card>
    </div>
  )
}
