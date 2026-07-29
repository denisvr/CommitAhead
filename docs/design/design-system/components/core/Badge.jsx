import React from 'react';

const tones = {
  critical: { color: 'var(--critical)', background: 'var(--critical-wash)' },
  caution: { color: 'var(--caution)', background: 'var(--caution-wash)' },
  good: { color: 'var(--good)', background: 'var(--good-wash)' },
  draft: { color: 'var(--accent)', background: 'var(--accent-wash)' },
  neutral: { color: 'var(--text-muted)', background: 'var(--surface-alt)' },
};

export function Badge({ children, tone = 'neutral', dot = true, style, ...rest }) {
  const t = tones[tone];
  return (
    <span
      style={{
        display: 'inline-flex', alignItems: 'center', gap: 'var(--space-3)',
        fontSize: 'var(--text-xs)', fontWeight: 'var(--weight-semibold)',
        borderRadius: 'var(--radius-xs)', padding: '4px 9px', whiteSpace: 'nowrap',
        ...t, ...style,
      }}
      {...rest}
    >
      {dot ? <span style={{ width: 6, height: 6, borderRadius: '50%', background: 'currentColor', display: 'block' }} /> : null}
      {children}
    </span>
  );
}
