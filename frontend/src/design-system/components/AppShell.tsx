import type { ReactNode } from 'react'
import styles from './AppShell.module.css'

export type NavDestination = {
  key: string
  label: string
}

type AppShellProps = {
  destinations: NavDestination[]
  activeDestination: string
  onNavigate: (key: string) => void
  email: string
  onLogout: () => void
  isLoggingOut: boolean
  children: ReactNode
}

// Owns the desktop sidebar / responsive mobile bar (components.md "AppShell"). Only lists
// destinations this slice actually built — StudyItem categories are filters, never nav
// destinations, and the remaining product areas (profile, job analyses, ...) aren't built yet.
export function AppShell({ destinations, activeDestination, onNavigate, email, onLogout, isLoggingOut, children }: AppShellProps) {
  return (
    <div className={styles.shell}>
      <aside className={styles.sidebar}>
        <div className={styles.brand}>
          <svg className={styles.mark} viewBox="0 0 32 46" width="16" height="23" fill="currentColor" fillRule="evenodd" role="img" aria-label="CommitAhead">
            <path d="M2 0h28a2 2 0 0 1 2 2v44l-9.6-11.4h-4L0 46V2a2 2 0 0 1 2-2Z M6 11.5h20v3.5H6z" />
          </svg>
          <span className={styles.wordmark}>
            <span className={styles.wordmarkStrong}>Commit</span>
            <span className={styles.wordmarkLight}>Ahead</span>
          </span>
        </div>
        <nav className={styles.nav} aria-label="Primary">
          {destinations.map((destination) => (
            <button
              key={destination.key}
              type="button"
              className={[styles.navLink, destination.key === activeDestination ? styles.navLinkActive : ''].join(' ').trim()}
              aria-current={destination.key === activeDestination ? 'page' : undefined}
              onClick={() => onNavigate(destination.key)}
            >
              {destination.label}
            </button>
          ))}
        </nav>
        <div className={styles.footer}>
          <p className={styles.email}>{email}</p>
          <button type="button" className={styles.navLink} onClick={onLogout} disabled={isLoggingOut}>
            Log out
          </button>
        </div>
      </aside>
      <main className={styles.content}>{children}</main>
    </div>
  )
}
