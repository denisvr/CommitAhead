import { useId, type ReactElement, type ReactNode } from 'react'
import styles from './Field.module.css'

type FieldProps = {
  label: string
  hint?: string
  error?: string
  children: (fieldProps: { id: string; 'aria-describedby'?: string; 'aria-invalid'?: boolean }) => ReactElement
}

// Field owns id/aria wiring so every input in the app gets a real programmatic label and error
// association without each call site re-deriving useId plumbing (components.md's Field contract).
export function Field({ label, hint, error, children }: FieldProps): ReactNode {
  const id = useId()
  const hintId = hint ? `${id}-hint` : undefined
  const errorId = error ? `${id}-error` : undefined
  const describedBy = [hintId, errorId].filter(Boolean).join(' ') || undefined

  return (
    <div className={styles.field}>
      <label htmlFor={id} className={styles.label}>
        {label}
      </label>
      {children({ id, 'aria-describedby': describedBy, 'aria-invalid': error ? true : undefined })}
      {hint && !error && (
        <p id={hintId} className={styles.hint}>
          {hint}
        </p>
      )}
      {error && (
        <p id={errorId} className={styles.error} role="alert">
          {error}
        </p>
      )}
    </div>
  )
}
