import { useEffect, useRef, useState } from 'react'
import { Icon } from '../Icon'
import styles from './AccountMenu.module.css'

type AccountMenuProps = {
  email: string
  onLogout: () => void
  isLoggingOut: boolean
}

// Splits on the separators an email local-part actually uses (denis.teste -> "DT") rather than
// fabricating a display name CommitAhead doesn't have — /api/me returns only an email.
function getInitials(email: string): string {
  const localPart = email.split('@')[0] ?? ''
  const segments = localPart.split(/[._-]+/).filter(Boolean)
  if (segments.length >= 2) return (segments[0][0] + segments[1][0]).toUpperCase()
  return localPart.slice(0, 2).toUpperCase() || '?'
}

// A circular avatar that expands into an account panel on click — the interaction Azure DevOps
// uses for its own account control, at the user's explicit request. The panel shows only what
// CommitAhead actually has (email, log out) — no invented "switch directory" or multi-account
// list; there is exactly one real user and no such feature (CLAUDE.md).
export function AccountMenu({ email, onLogout, isLoggingOut }: AccountMenuProps) {
  const [open, setOpen] = useState(false)
  const containerRef = useRef<HTMLDivElement>(null)
  const initials = getInitials(email)

  useEffect(() => {
    if (!open) return

    const handlePointerDown = (event: MouseEvent) => {
      if (containerRef.current && !containerRef.current.contains(event.target as Node)) setOpen(false)
    }
    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') setOpen(false)
    }

    document.addEventListener('mousedown', handlePointerDown)
    document.addEventListener('keydown', handleKeyDown)
    return () => {
      document.removeEventListener('mousedown', handlePointerDown)
      document.removeEventListener('keydown', handleKeyDown)
    }
  }, [open])

  return (
    <div className={styles.container} ref={containerRef}>
      <button type="button" className={styles.avatar} onClick={() => setOpen((current) => !current)} aria-haspopup="true" aria-expanded={open} aria-label="Account menu">
        {initials}
      </button>

      {open && (
        <div className={styles.panel}>
          <div className={styles.panelHeader}>
            <span className={styles.avatarLarge} aria-hidden="true">
              {initials}
            </span>
            <span className={styles.email}>{email}</span>
          </div>
          <button type="button" className={styles.logoutButton} onClick={onLogout} disabled={isLoggingOut}>
            <Icon name="log-out" />
            {isLoggingOut ? 'Signing out…' : 'Log out'}
          </button>
        </div>
      )}
    </div>
  )
}
