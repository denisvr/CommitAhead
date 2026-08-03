import type { KeyboardEvent } from 'react'
import { useRef } from 'react'
import styles from './Tabs.module.css'

export type TabDefinition = {
  key: string
  label: string
}

type TabsProps = {
  tabs: TabDefinition[]
  activeTab: string
  onChange: (key: string) => void
  'aria-label': string
}

// The ARIA tabs pattern (components.md "Tabs"): role="tablist"/"tab", aria-selected, roving
// tabindex, and arrow-key navigation. The caller owns the single tabpanel each tab controls.
export function Tabs({ tabs, activeTab, onChange, 'aria-label': ariaLabel }: TabsProps) {
  const tabRefs = useRef<Record<string, HTMLButtonElement | null>>({})

  const handleKeyDown = (event: KeyboardEvent<HTMLButtonElement>, index: number) => {
    if (event.key !== 'ArrowRight' && event.key !== 'ArrowLeft') {
      return
    }

    event.preventDefault()
    const delta = event.key === 'ArrowRight' ? 1 : -1
    const nextTab = tabs[(index + delta + tabs.length) % tabs.length]
    onChange(nextTab.key)
    tabRefs.current[nextTab.key]?.focus()
  }

  return (
    <div className={styles.tablist} role="tablist" aria-label={ariaLabel}>
      {tabs.map((tab, index) => (
        <button
          key={tab.key}
          ref={(element) => {
            tabRefs.current[tab.key] = element
          }}
          type="button"
          role="tab"
          id={`tab-${tab.key}`}
          aria-selected={tab.key === activeTab}
          aria-controls={`tabpanel-${tab.key}`}
          tabIndex={tab.key === activeTab ? 0 : -1}
          className={[styles.tab, tab.key === activeTab ? styles.tabActive : ''].join(' ').trim()}
          onClick={() => onChange(tab.key)}
          onKeyDown={(event) => handleKeyDown(event, index)}
        >
          {tab.label}
        </button>
      ))}
    </div>
  )
}
