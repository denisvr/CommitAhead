import React from 'react';
import { Icon } from './Icon.jsx';

export function IconButton({ icon, label, size = 'md', tone = 'default', disabled, style, ...rest }) {
  const [hover, setHover] = React.useState(false);
  const dim = size === 'sm' ? 'var(--control-height-sm)' : 'var(--control-height)';
  const color = tone === 'danger' ? 'var(--critical)' : 'var(--text-muted)';
  return (
    <button
      aria-label={label}
      title={label}
      disabled={disabled}
      onMouseEnter={() => setHover(true)}
      onMouseLeave={() => setHover(false)}
      style={{
        width: dim, height: dim, display: 'inline-flex', alignItems: 'center', justifyContent: 'center',
        borderRadius: 'var(--radius-sm)', border: '1px solid transparent',
        background: hover && !disabled ? (tone === 'danger' ? 'var(--critical-wash)' : 'var(--surface-alt)') : 'transparent',
        color: hover && !disabled && tone !== 'danger' ? 'var(--text)' : color,
        cursor: disabled ? 'not-allowed' : 'pointer', opacity: disabled ? 0.45 : 1,
        transition: 'background-color var(--dur-fast) var(--ease-standard), color var(--dur-fast) var(--ease-standard)',
        ...style,
      }}
      {...rest}
    >
      <Icon name={icon} size={size === 'sm' ? 15 : 17} />
    </button>
  );
}
