import React from 'react';

export function Textarea({ invalid, rows = 6, mono, style, ...rest }) {
  return (
    <textarea
      rows={rows}
      style={{
        width: '100%', padding: '10px 12px', resize: 'vertical',
        fontFamily: mono ? 'var(--font-mono)' : 'var(--font-sans)',
        fontSize: 'var(--text-sm)', lineHeight: 'var(--leading-prose)', color: 'var(--text)',
        background: 'var(--surface)',
        border: '1px solid ' + (invalid ? 'var(--critical)' : 'var(--border-strong)'),
        borderRadius: 'var(--radius-sm)',
        ...style,
      }}
      {...rest}
    />
  );
}
