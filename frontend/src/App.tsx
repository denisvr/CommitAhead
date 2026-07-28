import { useEffect, useState } from 'react'
import { apiClient } from './api/client'
import { LoginForm } from './features/auth/LoginForm'

type AuthState = 'loading' | 'authenticated' | 'anonymous'

function App() {
  const [authState, setAuthState] = useState<AuthState>('loading')
  const [email, setEmail] = useState<string | null>(null)

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
    const { data: csrf } = await apiClient.GET('/auth/csrf')
    if (!csrf) {
      return
    }

    await apiClient.POST('/auth/logout', {
      headers: { 'X-CSRF-TOKEN': csrf.token },
    })

    setEmail(null)
    setAuthState('anonymous')
  }

  if (authState === 'loading') {
    return (
      <main>
        <h1>CommitAhead</h1>
      </main>
    )
  }

  return (
    <main>
      <h1>CommitAhead</h1>
      {authState === 'anonymous' ? (
        <LoginForm />
      ) : (
        <>
          <p>Signed in as {email}</p>
          <button type="button" onClick={handleLogout}>
            Log out
          </button>
        </>
      )}
    </main>
  )
}

export default App
