import React from 'react';

export function DataTable({ columns, rows, onRowClick, style, ...rest }) {
  const grid = columns.map((c) => c.width || '1fr').join(' ');
  return (
    <div data-density="dense" style={{ ...style }} {...rest}>
      <div style={{ display: 'grid', gridTemplateColumns: grid, gap: 12, padding: '8px 10px', borderBottom: '1px solid var(--border)', fontFamily: 'var(--font-mono)', fontWeight: 'var(--weight-medium)', fontSize: 'var(--text-micro)', letterSpacing: 'var(--track-label)', textTransform: 'uppercase', color: 'var(--text-faint)' }}>
        {columns.map((c) => <span key={c.key} style={{ textAlign: c.align || 'left' }}>{c.label}</span>)}
      </div>
      {rows.map((r, i) => (
        <div
          key={r.id || i}
          onClick={onRowClick ? () => onRowClick(r) : undefined}
          style={{
            display: 'grid', gridTemplateColumns: grid, gap: 12, alignItems: 'center',
            padding: '7px 10px', borderBottom: '1px solid var(--border-soft)',
            fontSize: 'var(--text-sm)', cursor: onRowClick ? 'pointer' : 'default',
            background: i % 2 ? 'var(--surface-alt)' : 'transparent',
          }}
        >
          {columns.map((c) => (
            <span key={c.key} style={{ textAlign: c.align || 'left', fontFamily: c.mono ? 'var(--font-mono)' : 'inherit', fontVariantNumeric: c.mono ? 'tabular-nums' : undefined, color: c.muted ? 'var(--text-muted)' : 'var(--text)', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
              {r[c.key]}
            </span>
          ))}
        </div>
      ))}
    </div>
  );
}
