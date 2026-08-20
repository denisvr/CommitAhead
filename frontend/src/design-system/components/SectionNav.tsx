import { useEffect, useRef, useState } from 'react'
import styles from './SectionNav.module.css'

export type SectionNavItem = {
  key: string
  label: string
  count?: number
  severity?: 'critical' | 'caution'
}

type SectionNavProps = {
  items: SectionNavItem[]
  'aria-label': string
}

// The sticky jump bar (components.md "SectionNav") — links with a scroll-spy aria-current, not
// tabs: it navigates within one page rather than swapping panels, so it deliberately does not
// implement the ARIA tabs pattern (no role="tablist", no arrow-key roving tabindex).
export function SectionNav({ items, 'aria-label': ariaLabel }: SectionNavProps) {
  const [active, setActive] = useState(items[0]?.key)
  const linkRefs = useRef<Record<string, HTMLAnchorElement | null>>({})

  useEffect(() => {
    // Not available in the jsdom test environment — the jump bar still works via its onClick
    // handlers, it just won't track scroll position there.
    if (typeof IntersectionObserver === 'undefined') return

    const sections = items.map((item) => document.getElementById(item.key)).filter((el): el is HTMLElement => el !== null)
    if (sections.length === 0) return

    const observer = new IntersectionObserver(
      (entries) => {
        for (const entry of entries) {
          if (entry.isIntersecting) {
            setActive(entry.target.id)
            linkRefs.current[entry.target.id]?.scrollIntoView?.({ inline: 'nearest', block: 'nearest' })
          }
        }
      },
      { rootMargin: '-45% 0px -50% 0px' },
    )

    sections.forEach((section) => observer.observe(section))
    return () => observer.disconnect()
  }, [items])

  return (
    <nav className={styles.nav} aria-label={ariaLabel}>
      <div className={styles.scroller}>
        {items.map((item) => (
          <a
            key={item.key}
            ref={(element) => {
              linkRefs.current[item.key] = element
            }}
            href={`#${item.key}`}
            className={styles.link}
            aria-current={item.key === active ? 'true' : undefined}
            onClick={(event) => {
              event.preventDefault()
              document.getElementById(item.key)?.scrollIntoView?.({ behavior: 'smooth', block: 'start' })
            }}
          >
            {item.severity && <span className={[styles.flag, styles[item.severity]].join(' ')} aria-hidden="true" />}
            {item.label}
            {item.count !== undefined && <span className={styles.count}>{item.count}</span>}
          </a>
        ))}
      </div>
    </nav>
  )
}
