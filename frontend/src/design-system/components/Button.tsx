import type { ButtonHTMLAttributes } from 'react'
import styles from './Button.module.css'

type ButtonVariant = 'primary' | 'secondary' | 'ghost' | 'danger'

type ButtonProps = ButtonHTMLAttributes<HTMLButtonElement> & {
  variant?: ButtonVariant
  isLoading?: boolean
}

export function Button({ variant = 'secondary', isLoading = false, disabled, className, children, ...rest }: ButtonProps) {
  return (
    <button
      type="button"
      className={[styles.button, styles[variant], className].filter(Boolean).join(' ')}
      disabled={disabled || isLoading}
      aria-busy={isLoading || undefined}
      {...rest}
    >
      <span className={isLoading ? styles.hiddenLabel : undefined}>{children}</span>
      {isLoading && <span className={styles.spinner} aria-hidden="true" />}
    </button>
  )
}
