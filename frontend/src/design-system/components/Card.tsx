import { useState, type ReactNode } from 'react'
import { Icon, type IconName } from '../Icon'
import styles from './Card.module.css'

type CardProps = {
  id?: string
  icon: IconName
  heading: string
  meta?: ReactNode
  badge?: ReactNode
  lead?: ReactNode
  actions?: ReactNode
  children: ReactNode
}

// The primary structural surface (components.md "Card") — one profile section per card. Never
// nested inside another card; sections are separated by --card-gap whitespace, not a rule.
//
// Collapsible at the whole-section level (new chrome, not in cv-3-studio-v2.1.html, at the user's
// request) — a sibling toggle button next to `actions`/`badge` rather than making `.head` itself
// a button, since `actions` can hold real interactive controls and a button can't nest a button.
// Deliberately plain useState, no persistence: a full page reload resets every card to expanded.
export function Card({ id, icon, heading, meta, badge, lead, actions, children }: CardProps) {
  const [open, setOpen] = useState(true)

  return (
    <section id={id} className={styles.card}>
      <div className={styles.head}>
        <div className={styles.iconBox}>
          <Icon name={icon} />
        </div>
        <div className={styles.headText}>
          <h2 className={styles.heading}>{heading}</h2>
          {meta && <p className={styles.meta}>{meta}</p>}
        </div>
        {actions && <div className={styles.actions}>{actions}</div>}
        {badge && <div className={styles.badgeSlot}>{badge}</div>}
        <button
          type="button"
          className={styles.collapseToggle}
          onClick={() => setOpen((value) => !value)}
          aria-expanded={open}
          aria-label={open ? `Collapse ${heading}` : `Expand ${heading}`}
        >
          <Icon name={open ? 'chevron-up' : 'chevron-down'} />
        </button>
      </div>
      {open && (
        <div className={styles.body}>
          {lead && <p className={styles.lead}>{lead}</p>}
          {children}
        </div>
      )}
    </section>
  )
}
