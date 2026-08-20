import type { ReactNode } from 'react'
import styles from './Badge.module.css'

type BadgeTone = 'critical' | 'caution' | 'good' | 'neutral'

// Three severities (components.md "Chip and Badge") — the words carry the meaning as much as the
// colour does, so callers pass real sentences ("2 missing verification links"), never a bare
// category name. `size="sm"` matches the approved reference's smaller row-level status badge
// (".mini") as distinct from the card-level one (".badge").
export function Badge({ tone, size = 'default', children }: { tone: BadgeTone; size?: 'default' | 'sm'; children: ReactNode }) {
  return <span className={[styles.badge, styles[tone], size === 'sm' ? styles.sm : ''].join(' ').trim()}>{children}</span>
}
