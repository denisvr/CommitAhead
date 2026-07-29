import React from 'react';
import { Icon } from './Icon.jsx';

const tones = {
  info: { border: 'var(--accent)', icon: 'circle-alert', color: 'var(--accent)' },
  critical: { border: 'var(--critical)', icon: 'circle-alert', color: 'var(--critical)' },
  caution: { border: 'var(--caution)', icon: 'circle-alert', color: 'var(--caution)' },
};

export function Callout({ children, title, tone = 'info', style, ...rest }) {
  const t = tones[tone];
  return (
    <div
      role={tone === 'critical' ? 'alert' : undefined}
      style={{
        display: 'flex', gap: 'var(--space-6)', padding: 'var(--space-7) var(--space-8)',
        border: '1px solid var(--border)', borderLeft: '3px solid ' + t.border,
        borderRadius: 'var(--radius-sm)', background: 'var(--surface)',
        fontSize: 'var(--text-sm)', lineHeight: 'var(--leading-prose)', color: 'var(--text-muted)',
        ...style,
      }}
      {...rest}
    >
      <span style={{ color: t.color, paddingTop: 2 }}><Icon name={t.icon} size={16} /></span>
      <div>
        {title ? <div style={{ color: 'var(--text)', fontWeight: 'var(--weight-semibold)', marginBottom: 4 }}>{title}</div> : null}
        {children}
      </div>
    </div>
  );
}
