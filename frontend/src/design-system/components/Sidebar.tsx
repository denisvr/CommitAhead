import { Icon, type IconName } from '../Icon'
import styles from './Sidebar.module.css'

export type SidebarItem = {
  key: string
  label: string
  icon: IconName
}

type SidebarProps = {
  items: SidebarItem[]
  activeKey: string | null
  onNavigate: (key: string) => void
  collapsed: boolean
  onToggleCollapsed: () => void
}

// A persistent left navigation rail (components.md "Sidebar") — modelled on Azure DevOps's own
// project sidebar at the user's explicit request, not on cv-3-studio-v2.1.html, which has no such
// rail. Items use full feature names, not abbreviations (e.g. "CV presentations", not "CV") —
// the sidebar is the primary place a first-time user learns what the app offers.
export function Sidebar({ items, activeKey, onNavigate, collapsed, onToggleCollapsed }: SidebarProps) {
  return (
    <nav className={[styles.sidebar, collapsed ? styles.collapsed : ''].join(' ').trim()} aria-label="Primary">
      <ul className={styles.list}>
        {items.map((item) => (
          <li key={item.key}>
            <button
              type="button"
              className={[styles.item, item.key === activeKey ? styles.itemActive : ''].join(' ').trim()}
              aria-current={item.key === activeKey ? 'page' : undefined}
              // The visible label is display:none whenever collapsed (desktop) OR below 767px
              // (mobile forces icon-only regardless of the collapse preference — Sidebar.module.css's
              // own media query) — an explicit aria-label, not `title` alone, keeps the button's
              // accessible name correct in every one of those cases, not just the ones `collapsed`
              // happens to know about. `title` stays as a supplementary hover tooltip only.
              aria-label={item.label}
              title={collapsed ? item.label : undefined}
              onClick={() => onNavigate(item.key)}
            >
              <Icon name={item.icon} className={styles.itemIcon} />
              <span className={styles.itemLabel} aria-hidden="true">
                {item.label}
              </span>
            </button>
          </li>
        ))}
      </ul>

      <button type="button" className={styles.collapseToggle} onClick={onToggleCollapsed} aria-label={collapsed ? 'Expand sidebar' : 'Collapse sidebar'}>
        <Icon name="chevrons-left" className={collapsed ? styles.collapseIconFlipped : undefined} />
        <span className={styles.collapseLabel}>Collapse</span>
      </button>
    </nav>
  )
}
