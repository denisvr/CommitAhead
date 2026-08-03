import { useEffect, useState } from 'react'
import { apiClient, ensureFreshSession } from './api/client'
import { AppShell } from './design-system/components/AppShell'
import { BookmarkMark } from './design-system/components/Brand'
import { Button } from './design-system/components/Button'
import { LoginForm } from './features/auth/LoginForm'
import { ScoringSettingsPage } from './features/settings/ScoringSettingsPage'
import { NewStudyItemPage } from './features/study-items/NewStudyItemPage'
import { StudyItemDetailPage } from './features/study-items/StudyItemDetailPage'
import { StudyItemsListPage } from './features/study-items/StudyItemsListPage'
import { StudyQueuePage } from './features/study-items/StudyQueuePage'
import styles from './App.module.css'

// 'connection-error' is distinct from 'anonymous' on purpose — a network failure while checking
// /api/me proves nothing about whether a session exists, so it must never be treated as a
// confirmed logged-out state (that would show the login form over what might be a live session).
type AuthState = 'loading' | 'authenticated' | 'anonymous' | 'connection-error'

// "from" remembers which list a detail/creation flow was opened from, so Back/Delete/Created
// return there instead of always assuming the ranked queue.
type Origin = 'queue' | 'items'

type View =
  | { name: 'queue' }
  | { name: 'items' }
  | { name: 'settings' }
  | { name: 'detail'; id: string; from: Origin }
  | { name: 'new'; from: Origin }

function originView(origin: Origin): View {
  return origin === 'items' ? { name: 'items' } : { name: 'queue' }
}

function describeActiveDestination(view: View): 'queue' | 'items' | 'settings' {
  if (view.name === 'items' || view.name === 'settings') {
    return view.name
  }

  if (view.name === 'detail' || view.name === 'new') {
    return view.from === 'items' ? 'items' : 'queue'
  }

  return 'queue'
}

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
  const [view, setView] = useState<View>({ name: 'queue' })

  // Inlined rather than sharing a callback with retryConnection — see the study-items pages for
  // why (the set-state-in-effect lint rule treats any call to a state-setting function reference
  // as synchronous, regardless of the await inside it).
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

  const activeDestination = describeActiveDestination(view)

  return (
    <AppShell
      destinations={[
        { key: 'queue', label: 'Study queue' },
        { key: 'items', label: 'Study items' },
        { key: 'settings', label: 'Settings' },
      ]}
      activeDestination={activeDestination}
      onNavigate={(key) => setView(key === 'settings' ? { name: 'settings' } : key === 'items' ? { name: 'items' } : { name: 'queue' })}
      email={email ?? ''}
      onLogout={handleLogout}
      isLoggingOut={isLoggingOut}
    >
      {logoutError && <p role="alert">{logoutError}</p>}
      {view.name === 'queue' && (
        <StudyQueuePage onSelectItem={(id) => setView({ name: 'detail', id, from: 'queue' })} onCreateNew={() => setView({ name: 'new', from: 'queue' })} />
      )}
      {view.name === 'items' && (
        <StudyItemsListPage onSelectItem={(id) => setView({ name: 'detail', id, from: 'items' })} onCreateNew={() => setView({ name: 'new', from: 'items' })} />
      )}
      {view.name === 'settings' && <ScoringSettingsPage />}
      {view.name === 'detail' && (
        <StudyItemDetailPage
          key={view.id}
          itemId={view.id}
          onBack={() => setView(originView(view.from))}
          onDeleted={() => setView(originView(view.from))}
        />
      )}
      {view.name === 'new' && (
        <NewStudyItemPage onCreated={(id) => setView({ name: 'detail', id, from: view.from })} onCancel={() => setView(originView(view.from))} />
      )}
    </AppShell>
  )
}

export default App
