import { useState, type FormEvent } from 'react'
import { apiClient } from '../../api/client'
import { Button } from '../../design-system/components/Button'
import { Field } from '../../design-system/components/Field'
import inputStyles from '../../design-system/components/Input.module.css'
import styles from './LoginForm.module.css'

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
    return <p className={styles.message}>{message}</p>
  }

  return (
    <form className={styles.form} onSubmit={handleSubmit}>
      <Field label="Email">
        {(fieldProps) => (
          <input {...fieldProps} type="email" required className={inputStyles.input} value={email} onChange={(event) => setEmail(event.target.value)} />
        )}
      </Field>
      <Button type="submit" variant="primary" isLoading={isSubmitting}>
        Send sign-in link
      </Button>
    </form>
  )
}
