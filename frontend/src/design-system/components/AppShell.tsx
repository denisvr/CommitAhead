import type { ReactNode } from 'react'
import { Brand } from './Brand'
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

// Owns the desktop sidebar and the mobile top bar + bottom nav (components.md "AppShell",
// page-patterns.md "mobile replaces the sidebar below 768px; it does not merely squeeze it").
// Renders the same destinations twice — once for the desktop sidebar, once for the mobile bottom
// nav — each hidden by the other viewport's media query rather than by JS breakpoint detection.
// Only lists destinations this slice actually built — StudyItem categories are filters, never nav
// destinations, and the remaining product areas (profile, job analyses, ...) aren't built yet.
export function AppShell({ destinations, activeDestination, onNavigate, email, onLogout, isLoggingOut, children }: AppShellProps) {
  const renderNavButton = (destination: NavDestination) => (
    <button
      key={destination.key}
      type="button"
      className={[styles.navLink, destination.key === activeDestination ? styles.navLinkActive : ''].join(' ').trim()}
      aria-current={destination.key === activeDestination ? 'page' : undefined}
      onClick={() => onNavigate(destination.key)}
    >
      {destination.label}
    </button>
  )

  return (
    <div className={styles.shell}>
      <aside className={styles.sidebar}>
        <Brand />
        <nav className={styles.nav} aria-label="Primary">
          {destinations.map(renderNavButton)}
        </nav>
        <div className={styles.footer}>
          <p className={styles.email}>{email}</p>
          <button type="button" className={styles.navLink} onClick={onLogout} disabled={isLoggingOut}>
            Log out
          </button>
        </div>
      </aside>

      <header className={styles.mobileHeader}>
        <Brand className={styles.mobileBrand} />
        <button type="button" className={styles.mobileLogout} onClick={onLogout} disabled={isLoggingOut}>
          Log out
        </button>
      </header>

      <main className={styles.content}>{children}</main>

      <nav className={styles.mobileNav} aria-label="Primary">
        {destinations.map(renderNavButton)}
      </nav>
    </div>
  )
}
