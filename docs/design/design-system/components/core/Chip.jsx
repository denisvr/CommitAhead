import React from 'react';

export function Chip({ children, selected, as = 'span', onClick, style, ...rest }) {
  const Tag = onClick ? 'button' : as;
  return (
    <Tag
      onClick={onClick}
      style={{
        display: 'inline-flex', alignItems: 'center',
        fontFamily: 'var(--font-mono)', fontSize: 'var(--text-xs)', lineHeight: 1,
        color: selected ? 'var(--accent-contrast)' : 'var(--text-muted)',
        background: selected ? 'var(--accent)' : 'transparent',
        border: '1px solid ' + (selected ? 'var(--accent)' : 'var(--border-strong)'),
        borderRadius: 'var(--radius-xs)', padding: '5px 8px', whiteSpace: 'nowrap',
        cursor: onClick ? 'pointer' : 'default',
        transition: 'background-color var(--dur-fast) var(--ease-standard), border-color var(--dur-fast) var(--ease-standard)',
        ...style,
      }}
      {...rest}
    >
      {children}
    </Tag>
  );
}
