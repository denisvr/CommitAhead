import React from 'react';

export function EmptyState({ title, children, action, style, ...rest }) {
  return (
    <div style={{ padding: 'var(--space-22) var(--space-8)', textAlign: 'center', ...style }} {...rest}>
      <div style={{ fontSize: 'var(--text-lead)', fontWeight: 'var(--weight-semibold)', letterSpacing: 'var(--track-headline)', marginBottom: 'var(--space-3)' }}>{title}</div>
      <div style={{ fontSize: 'var(--text-sm)', color: 'var(--text-muted)', maxWidth: '46ch', margin: '0 auto var(--space-8)', lineHeight: 'var(--leading-prose)' }}>{children}</div>
      {action}
    </div>
  );
}
