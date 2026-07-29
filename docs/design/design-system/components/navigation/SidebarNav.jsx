import React from 'react';
import { Icon } from '../core/Icon.jsx';
import { Brand } from './Brand.jsx';

export const NAV_ITEMS = [
  { id: 'queue', label: 'Study Queue', icon: 'list-ordered' },
  { id: 'items', label: 'Study Items', icon: 'book-marked' },
  { id: 'profile', label: 'Profile & CVs', icon: 'user-round' },
  { id: 'jobs', label: 'Job Analyses', icon: 'briefcase' },
  { id: 'notes', label: 'Interview Notes', icon: 'notebook-text' },
  { id: 'settings', label: 'Settings', icon: 'settings' },
];

export function SidebarNav({ items = NAV_ITEMS, active, onNavigate, footer, style, ...rest }) {
  return (
    <aside
      style={{
        width: 'var(--sidebar-width)', flex: 'none', padding: 'var(--space-14) var(--space-6)',
        display: 'flex', flexDirection: 'column', gap: 'var(--space-14)', background: 'var(--bg)',
        ...style,
      }}
      {...rest}
    >
      <div style={{ padding: '0 var(--space-5)' }}><Brand /></div>
      <nav style={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
        {items.map((it) => {
          const on = it.id === active;
          return (
            <button
              key={it.id}
              onClick={() => onNavigate && onNavigate(it.id)}
              aria-current={on ? 'page' : undefined}
              style={{
                display: 'flex', alignItems: 'center', gap: 'var(--space-5)',
                padding: '9px var(--space-5)', minHeight: 38, border: 0, cursor: 'pointer', textAlign: 'left',
                borderRadius: 'var(--radius-sm)', fontFamily: 'var(--font-sans)', fontSize: 'var(--text-sm)',
                background: on ? 'var(--surface-alt)' : 'transparent',
                color: on ? 'var(--text)' : 'var(--text-muted)',
                fontWeight: on ? 'var(--weight-semibold)' : 'var(--weight-regular)',
              }}
            >
              <span style={{ color: on ? 'var(--accent)' : 'inherit', opacity: on ? 1 : 0.75 }}><Icon name={it.icon} size={16} /></span>
              {it.label}
            </button>
          );
        })}
      </nav>
      {footer ? <div style={{ marginTop: 'auto' }}>{footer}</div> : null}
    </aside>
  );
}
