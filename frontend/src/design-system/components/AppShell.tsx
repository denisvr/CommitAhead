import type { ReactNode } from 'react'
import { AccountMenu } from './AccountMenu'
import { BookmarkMark } from './Brand'
import { Sidebar, type SidebarItem } from './Sidebar'
import { ThemeToggle } from './ThemeToggle'
import styles from './AppShell.module.css'

type AppShellProps = {
  sidebarItems: SidebarItem[]
  activeSidebarItem: string | null
  onSidebarNavigate: (key: string) => void
  sidebarCollapsed: boolean
  onToggleSidebar: () => void
  // Clicking the brand mark/wordmark returns to Home, same destination as the Sidebar's own
  // "Home" item — a second, redundant entry point, not the only one, since DevOps's own header
  // brand behaves the same way alongside its nav rail.
  onHomeClick: () => void
  email: string
  onLogout: () => void
  isLoggingOut: boolean
  children: ReactNode
}

// Owns the sticky header, the primary navigation rail, the theme control and the content surface
// (components.md "AppShell", page-patterns.md "Application shell").
export function AppShell({
  sidebarItems,
  activeSidebarItem,
  onSidebarNavigate,
  sidebarCollapsed,
  onToggleSidebar,
  onHomeClick,
  email,
  onLogout,
  isLoggingOut,
  children,
}: AppShellProps) {
  return (
    <div className={styles.shell}>
      <header className={styles.header}>
        <div className={styles.headerInner}>
          <button type="button" className={styles.brand} onClick={onHomeClick} aria-label="CommitAhead home">
            <span className={styles.markBox}>
              <BookmarkMark className={styles.mark} size={17} />
            </span>
            <span className={styles.wordmark}>CommitAhead</span>
          </button>

          <span className={styles.spacer} />

          <ThemeToggle />
          <AccountMenu email={email} onLogout={onLogout} isLoggingOut={isLoggingOut} />
        </div>
      </header>

      <div className={[styles.body, sidebarCollapsed ? styles.bodyCollapsed : ''].filter(Boolean).join(' ')}>
        <Sidebar items={sidebarItems} activeKey={activeSidebarItem} onNavigate={onSidebarNavigate} collapsed={sidebarCollapsed} onToggleCollapsed={onToggleSidebar} />
        <main className={styles.content}>{children}</main>
      </div>
    </div>
  )
}
