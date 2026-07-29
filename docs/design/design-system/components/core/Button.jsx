import React from 'react';
import { Icon } from './Icon.jsx';

const base = {
  display: 'inline-flex', alignItems: 'center', justifyContent: 'center', gap: 'var(--space-4)',
  fontFamily: 'var(--font-sans)', fontWeight: 'var(--weight-semibold)', whiteSpace: 'nowrap',
  borderRadius: 'var(--radius-sm)', border: '1px solid transparent', cursor: 'pointer',
  transition: 'background-color var(--dur-fast) var(--ease-standard), color var(--dur-fast) var(--ease-standard), border-color var(--dur-fast) var(--ease-standard)',
};
const sizes = {
  md: { height: 'var(--control-height)', padding: '0 18px', fontSize: 'var(--text-sm)' },
  sm: { height: 'var(--control-height-sm)', padding: '0 12px', fontSize: 'var(--text-xs)' },
};
const variants = {
  primary: { background: 'var(--accent)', color: 'var(--accent-contrast)' },
  secondary: { background: 'transparent', color: 'var(--text-muted)', borderColor: 'var(--border-strong)', fontWeight: 'var(--weight-medium)' },
  ghost: { background: 'transparent', color: 'var(--text-muted)', fontWeight: 'var(--weight-medium)' },
  danger: { background: 'transparent', color: 'var(--critical)', borderColor: 'var(--critical)' },
};
const hovers = {
  primary: { background: 'var(--accent-hover)' },
  secondary: { background: 'var(--surface-alt)', color: 'var(--text)' },
  ghost: { background: 'var(--surface-alt)', color: 'var(--text)' },
  danger: { background: 'var(--critical-wash)' },
};

export function Button({ children, variant = 'primary', size = 'md', icon, iconEnd, disabled, fullWidth, style, ...rest }) {
  const [hover, setHover] = React.useState(false);
  return (
    <button
      disabled={disabled}
      onMouseEnter={() => setHover(true)}
      onMouseLeave={() => setHover(false)}
      style={{
        ...base, ...sizes[size], ...variants[variant],
        ...(hover && !disabled ? hovers[variant] : null),
        width: fullWidth ? '100%' : undefined,
        opacity: disabled ? 0.45 : 1,
        cursor: disabled ? 'not-allowed' : 'pointer',
        ...style,
      }}
      {...rest}
    >
      {icon ? <Icon name={icon} size={size === 'sm' ? 14 : 16} /> : null}
      {children}
      {iconEnd ? <Icon name={iconEnd} size={size === 'sm' ? 14 : 16} /> : null}
    </button>
  );
}
