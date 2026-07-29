import React from 'react';

export function Input({ invalid, style, ...rest }) {
  return (
    <input
      style={{
        width: '100%', height: 'var(--control-height)', padding: '0 12px',
        fontFamily: 'var(--font-sans)', fontSize: 'var(--text-sm)', color: 'var(--text)',
        background: 'var(--surface)',
        border: '1px solid ' + (invalid ? 'var(--critical)' : 'var(--border-strong)'),
        borderRadius: 'var(--radius-sm)',
        ...style,
      }}
      {...rest}
    />
  );
}
