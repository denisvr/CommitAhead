import React from 'react';

export function Field({ label, hint, error, htmlFor, children, style, ...rest }) {
  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-3)', ...style }} {...rest}>
      {label ? (
        <label htmlFor={htmlFor} style={{ fontSize: 'var(--text-xs)', color: 'var(--text-muted)', fontWeight: 'var(--weight-medium)' }}>{label}</label>
      ) : null}
      {children}
      {error ? (
        <span style={{ fontSize: 'var(--text-xs)', color: 'var(--critical)' }}>{error}</span>
      ) : hint ? (
        <span style={{ fontSize: 'var(--text-xs)', color: 'var(--text-faint)' }}>{hint}</span>
      ) : null}
    </div>
  );
}
