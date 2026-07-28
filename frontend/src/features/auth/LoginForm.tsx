import { useState, type FormEvent } from 'react'
import { apiClient } from '../../api/client'

export function LoginForm() {
  const [email, setEmail] = useState('')
  const [message, setMessage] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setIsSubmitting(true)

    const { data } = await apiClient.POST('/auth/login', {
      body: { email },
    })

    setMessage(data?.message ?? 'If that email is registered, a sign-in link has been sent.')
    setIsSubmitting(false)
  }

  if (message) {
    return <p>{message}</p>
  }

  return (
    <form onSubmit={handleSubmit}>
      <label htmlFor="email">Email</label>
      <input
        id="email"
        type="email"
        required
        value={email}
        onChange={(event) => setEmail(event.target.value)}
      />
      <button type="submit" disabled={isSubmitting}>
        Send sign-in link
      </button>
    </form>
  )
}
