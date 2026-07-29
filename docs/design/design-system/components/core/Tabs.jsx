import React from 'react';

export function Tabs({ items, value, onChange, style, ...rest }) {
  return (
    <div role="tablist" style={{ display: 'flex', gap: 'var(--space-12)', borderBottom: '1px solid var(--border-soft)', ...style }} {...rest}>
      {items.map((it) => {
        const on = it.value === value;
        return (
          <button
            key={it.value}
            role="tab"
            aria-selected={on}
            onClick={() => onChange && onChange(it.value)}
            style={{
              appearance: 'none', background: 'none', border: 0, cursor: 'pointer',
              padding: '0 0 10px', whiteSpace: 'nowrap', fontFamily: 'var(--font-sans)', fontSize: 'var(--text-sm)',
              fontWeight: on ? 'var(--weight-semibold)' : 'var(--weight-regular)',
              color: on ? 'var(--text)' : 'var(--text-muted)',
              borderBottom: '3px solid ' + (on ? 'var(--accent)' : 'transparent'),
              marginBottom: -1,
            }}
          >
            {it.label}
          </button>
        );
      })}
    </div>
  );
}
