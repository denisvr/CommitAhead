import { useEffect, useState } from 'react'
import { Icon, type IconName } from '../Icon'
import { applyThemePreference, readThemePreference, storeThemePreference, type ThemePreference } from '../theme'
import styles from './ThemeToggle.module.css'

const OPTIONS: { value: ThemePreference; label: string; icon: IconName }[] = [
  { value: 'light', label: 'Light', icon: 'sun' },
  { value: 'dark', label: 'Dark', icon: 'moon' },
  { value: 'system', label: 'Match system', icon: 'monitor' },
]

export function ThemeToggle() {
  const [preference, setPreference] = useState<ThemePreference>(readThemePreference)

  useEffect(() => {
    applyThemePreference(preference)
    storeThemePreference(preference)
  }, [preference])

  return (
    <div className={styles.toggle} role="group" aria-label="Theme">
      {OPTIONS.map((option) => (
        <button
          key={option.value}
          type="button"
          className={styles.option}
          aria-pressed={preference === option.value}
          title={option.label}
          onClick={() => setPreference(option.value)}
        >
          <Icon name={option.icon} />
          <span className={styles.label}>{option.label}</span>
        </button>
      ))}
    </div>
  )
}
