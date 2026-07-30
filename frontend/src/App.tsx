import { useEffect, useState } from 'react'
import { apiClient, ensureFreshSession } from './api/client'
import { AppShell } from './design-system/components/AppShell'
import { LoginForm } from './features/auth/LoginForm'
import { NewStudyItemPage } from './features/study-items/NewStudyItemPage'
import { StudyItemDetailPage } from './features/study-items/StudyItemDetailPage'
import { StudyQueuePage } from './features/study-items/StudyQueuePage'

type AuthState = 'loading' | 'authenticated' | 'anonymous'

type View = { name: 'queue' } | { name: 'detail'; id: string } | { name: 'new' }

function App() {
  const [authState, setAuthState] = useState<AuthState>('loading')
  const [email, setEmail] = useState<string | null>(null)
  const [logoutError, setLogoutError] = useState<string | null>(null)
  const [isLoggingOut, setIsLoggingOut] = useState(false)
  const [view, setView] = useState<View>({ name: 'queue' })

  useEffect(() => {
    apiClient.GET('/api/me').then(({ data, response }) => {
      if (response.status === 200 && data) {
        setEmail(data.email)
        setAuthState('authenticated')
      } else {
        setAuthState('anonymous')
      }
    })
  }, [])

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
      <main>
        <h1>CommitAhead</h1>
      </main>
    )
  }

  if (authState === 'anonymous') {
    return (
      <main>
        <h1>CommitAhead</h1>
        <LoginForm />
      </main>
    )
  }

  return (
    <AppShell
      destinations={[{ key: 'queue', label: 'Study queue' }]}
      activeDestination="queue"
      onNavigate={() => setView({ name: 'queue' })}
      email={email ?? ''}
      onLogout={handleLogout}
      isLoggingOut={isLoggingOut}
    >
      {logoutError && <p role="alert">{logoutError}</p>}
      {view.name === 'queue' && (
        <StudyQueuePage onSelectItem={(id) => setView({ name: 'detail', id })} onCreateNew={() => setView({ name: 'new' })} />
      )}
      {view.name === 'detail' && (
        <StudyItemDetailPage key={view.id} itemId={view.id} onBack={() => setView({ name: 'queue' })} onDeleted={() => setView({ name: 'queue' })} />
      )}
      {view.name === 'new' && <NewStudyItemPage onCreated={(id) => setView({ name: 'detail', id })} onCancel={() => setView({ name: 'queue' })} />}
    </AppShell>
  )
}

export default App
