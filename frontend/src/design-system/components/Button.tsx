import type { ButtonHTMLAttributes } from 'react'
import styles from './Button.module.css'

type ButtonVariant = 'primary' | 'secondary' | 'ghost' | 'danger' | 'success' | 'accent'
type ButtonSize = 'default' | 'sm' | 'icon'

type ButtonProps = ButtonHTMLAttributes<HTMLButtonElement> & {
  variant?: ButtonVariant
  size?: ButtonSize
  isLoading?: boolean
}

export function Button({ variant = 'secondary', size = 'default', isLoading = false, disabled, className, children, ...rest }: ButtonProps) {
  return (
    <button
      type="button"
      className={[styles.button, styles[variant], size !== 'default' ? styles[size] : '', className].filter(Boolean).join(' ')}
      disabled={disabled || isLoading}
      aria-busy={isLoading || undefined}
      {...rest}
    >
      <span className={isLoading ? styles.hiddenLabel : undefined}>{children}</span>
      {isLoading && <span className={styles.spinner} aria-hidden="true" />}
    </button>
  )
}
