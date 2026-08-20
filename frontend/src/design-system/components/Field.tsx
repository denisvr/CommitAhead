import { useId, type ReactElement, type ReactNode } from 'react'
import styles from './Field.module.css'

type FieldProps = {
  label: string
  required?: boolean
  // Trailing muted text on the same line as the label (cv-3-studio-v2.1.html "Headline * — your
  // title, not your dream job") — the reference never captions an optional field with the word
  // "Optional"; the absence of `required` already says that.
  inlineHint?: string
  hint?: string
  error?: string
  // For a caller placing this Field inside a shared grid (FormLayout.module.css ".grid") that
  // needs it to span the full width (".wide") — Field stays framework-agnostic about the grid
  // itself, the caller decides.
  className?: string
  children: (fieldProps: { id: string; 'aria-describedby'?: string; 'aria-invalid'?: boolean }) => ReactElement
}

// Field owns id/aria wiring so every input in the app gets a real programmatic label and error
// association without each call site re-deriving useId plumbing (components.md's Field contract).
export function Field({ label, required, inlineHint, hint, error, className, children }: FieldProps): ReactNode {
  const id = useId()
  const hintId = hint ? `${id}-hint` : undefined
  const errorId = error ? `${id}-error` : undefined
  const describedBy = [hintId, errorId].filter(Boolean).join(' ') || undefined

  return (
    <div className={[styles.field, className].filter(Boolean).join(' ')}>
      <label htmlFor={id} className={styles.label}>
        {label}
        {required && <i className={styles.required}>*</i>}
        {inlineHint && <span className={styles.inlineHint}> — {inlineHint}</span>}
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
