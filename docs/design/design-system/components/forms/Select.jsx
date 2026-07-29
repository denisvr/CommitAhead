import React from 'react';
import { Icon } from '../core/Icon.jsx';

export function Select({ options = [], style, ...rest }) {
  return (
    <span style={{ position: 'relative', display: 'block' }}>
      <select
        style={{
          width: '100%', height: 'var(--control-height)', padding: '0 34px 0 12px',
          appearance: 'none', fontFamily: 'var(--font-sans)', fontSize: 'var(--text-sm)',
          color: 'var(--text)', background: 'var(--surface)',
          border: '1px solid var(--border-strong)', borderRadius: 'var(--radius-sm)',
          ...style,
        }}
        {...rest}
      >
        {options.map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}
      </select>
      <span style={{ position: 'absolute', right: 11, top: '50%', transform: 'translateY(-50%)', color: 'var(--text-faint)', pointerEvents: 'none' }}>
        <Icon name="chevron-down" size={16} />
      </span>
    </span>
  );
}
