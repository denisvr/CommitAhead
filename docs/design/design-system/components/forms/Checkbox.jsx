import React from 'react';
import { Icon } from '../core/Icon.jsx';

export function Checkbox({ checked, onChange, label, disabled, style, ...rest }) {
  return (
    <label style={{ display: 'inline-flex', alignItems: 'center', gap: 'var(--space-4)', cursor: disabled ? 'not-allowed' : 'pointer', opacity: disabled ? 0.45 : 1, fontSize: 'var(--text-sm)', ...style }}>
      <input type="checkbox" checked={!!checked} onChange={onChange} disabled={disabled}
        style={{ position: 'absolute', opacity: 0, width: 18, height: 18, margin: 0, cursor: 'inherit' }} {...rest} />
      <span
        aria-hidden="true"
        style={{
          width: 18, height: 18, flex: 'none', display: 'inline-flex', alignItems: 'center', justifyContent: 'center',
          borderRadius: 'var(--radius-xs)',
          border: '1px solid ' + (checked ? 'var(--accent)' : 'var(--border-strong)'),
          background: checked ? 'var(--accent)' : 'var(--surface)',
          color: 'var(--accent-contrast)',
        }}
      >
        {checked ? <Icon name="check" size={13} strokeWidth={2.5} /> : null}
      </span>
      {label}
    </label>
  );
}
