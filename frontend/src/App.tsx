import { useEffect, useState } from 'react'
import { apiClient, ensureFreshSession } from './api/client'
import { AppShell } from './design-system/components/AppShell'
import { BookmarkMark } from './design-system/components/Brand'
import { Button } from './design-system/components/Button'
import type { SidebarItem } from './design-system/components/Sidebar'
import { readSidebarCollapsed, storeSidebarCollapsed } from './design-system/sidebar'
import { LoginForm } from './features/auth/LoginForm'
import { ProfileHubPage, type HubTab, type PresentationsView } from './features/professional-profile/ProfileHubPage'
import styles from './App.module.css'

// 'connection-error' is distinct from 'anonymous' on purpose — a network failure while checking
// /api/me proves nothing about whether a session exists, so it must never be treated as a
// confirmed logged-out state (that would show the login form over what might be a live session).
type AuthState = 'loading' | 'authenticated' | 'anonymous' | 'connection-error'

// Professional profile & CVs is the only feature in the app (Study, Job Analyses, Interview
// Notes, and the AI Analyze pipeline were removed — see docs/roadmap.md). Home is the hub's
// landing view, also reached via the brand mark; full names here, not abbreviations, per the
// user's own note that "CV" alone read as too terse.
const SIDEBAR_ITEMS: SidebarItem[] = [
  { key: 'home', label: 'Home', icon: 'house' },
  { key: 'profile', label: 'Professional profile', icon: 'user-round' },
  { key: 'presentations', label: 'CV presentations', icon: 'file-text' },
]

function AuthHeading() {
  return (
    <>
      <BookmarkMark className={styles.mark} size={32} />
      <h1 className={styles.heading}>
        <span className={styles.headingStrong}>Commit</span>
        <span className={styles.headingLight}>Ahead</span>
      </h1>
    </>
  )
}

function App() {
  const [authState, setAuthState] = useState<AuthState>('loading')
  const [email, setEmail] = useState<string | null>(null)
  const [logoutError, setLogoutError] = useState<string | null>(null)
  const [isLoggingOut, setIsLoggingOut] = useState(false)

  // Owned here, not in ProfileHubPage — the Sidebar and the page content it controls are siblings
  // under AppShell, so their shared state has to live in the nearest common ancestor.
  const [hubTab, setHubTab] = useState<HubTab>('home')
  const [presentationsView, setPresentationsView] = useState<PresentationsView>({ name: 'list' })
  const [sidebarCollapsed, setSidebarCollapsed] = useState(readSidebarCollapsed)

  const toggleSidebar = () => {
    setSidebarCollapsed((current) => {
      const next = !current
      storeSidebarCollapsed(next)
      return next
    })
  }

  const startNewCV = () => {
    setHubTab('presentations')
    setPresentationsView({ name: 'new' })
  }

  // Arriving at "CV presentations" from anywhere else always lands on the list — leaving a
  // half-finished "new" or a "detail" view behind and switching tabs must not resume it later.
  // Every entry point (Sidebar, the Home cards' "Open profile"/"View all", the create form's
  // "Go to your professional profile") funnels through this instead of the raw setter, except
  // startNewCV above, which explicitly wants "new" and sets it itself right after.
  const changeHubTab = (tab: HubTab) => {
    if (tab === 'presentations' && hubTab !== 'presentations') {
      setPresentationsView({ name: 'list' })
    }
    setHubTab(tab)
  }

  useEffect(() => {
    apiClient
      .GET('/api/me')
      .then(({ data, response }) => {
        if (response.status === 200 && data) {
          setEmail(data.email)
          setAuthState('authenticated')
        } else {
          setAuthState('anonymous')
        }
      })
      .catch(() => {
        setAuthState('connection-error')
      })
  }, [])

  const retryConnection = () => {
    setAuthState('loading')
    apiClient
      .GET('/api/me')
      .then(({ data, response }) => {
        if (response.status === 200 && data) {
          setEmail(data.email)
          setAuthState('authenticated')
        } else {
          setAuthState('anonymous')
        }
      })
      .catch(() => {
        setAuthState('connection-error')
      })
  }

  const handleLogout = async () => {
    setIsLoggingOut(true)
    setLogoutError(null)

    try {
      // Best-effort: a fresh access token gives /auth/logout a real Supabase token to revoke.
      // ensureFreshSession never rejects, so a refresh failure here can't abort the rest of logout.
      await ensureFreshSession()

      const { data: csrf, response: csrfResponse } = await apiClient.GET('/auth/csrf')
      if (!csrf || !csrfResponse.ok) {
        throw new Error('Could not obtain a CSRF token for logout.')
      }

      const { response: logoutResponse } = await apiClient.POST('/auth/logout', {
        headers: { 'X-CSRF-TOKEN': csrf.token },
      })
      if (!logoutResponse.ok) {
        throw new Error(`Logout request failed with status ${logoutResponse.status}.`)
      }

      setEmail(null)
      setAuthState('anonymous')
    } catch {
      // Logout never reached (or was not accepted by) the backend — keep showing the
      // authenticated UI rather than silently pretending the user is signed out, and let them
      // retry instead of getting stuck.
      setLogoutError('Something went wrong signing you out. Please try again.')
    } finally {
      setIsLoggingOut(false)
    }
  }

  if (authState === 'loading') {
    return (
      <main className={styles.authScreen}>
        <AuthHeading />
      </main>
    )
  }

  if (authState === 'connection-error') {
    return (
      <main className={styles.authScreen}>
        <AuthHeading />
        <p role="alert" className={styles.message}>
          Could not reach CommitAhead. Check your connection and try again.
        </p>
        <Button onClick={retryConnection}>Try again</Button>
      </main>
    )
  }

  if (authState === 'anonymous') {
    return (
      <main className={styles.authScreen}>
        <AuthHeading />
        <LoginForm />
      </main>
    )
  }

  return (
    <AppShell
      sidebarItems={SIDEBAR_ITEMS}
      activeSidebarItem={hubTab}
      onSidebarNavigate={(key) => changeHubTab(key as HubTab)}
      sidebarCollapsed={sidebarCollapsed}
      onToggleSidebar={toggleSidebar}
      onHomeClick={() => changeHubTab('home')}
      email={email ?? ''}
      onLogout={handleLogout}
      isLoggingOut={isLoggingOut}
    >
      {logoutError && <p role="alert">{logoutError}</p>}
      <ProfileHubPage
        hubTab={hubTab}
        onHubTabChange={changeHubTab}
        presentationsView={presentationsView}
        onPresentationsViewChange={setPresentationsView}
        onCreateCV={startNewCV}
      />
    </AppShell>
  )
}

export default App
