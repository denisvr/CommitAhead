import React from 'react';

export function ScoreNumeral({ value, label = 'Effective score', size = 52, align = 'right', style, ...rest }) {
  return (
    <div style={{ textAlign: align, ...style }} {...rest}>
      <div style={{ fontFamily: 'var(--font-mono)', fontSize: size, lineHeight: 1, letterSpacing: '-0.03em', fontVariantNumeric: 'tabular-nums', color: 'var(--text)' }}>{value}</div>
      {label ? (
        <div style={{ marginTop: 'var(--space-4)', fontFamily: 'var(--font-mono)', fontWeight: 'var(--weight-medium)', fontSize: 'var(--text-micro)', letterSpacing: 'var(--track-label)', textTransform: 'uppercase', color: 'var(--text-faint)' }}>{label}</div>
      ) : null}
    </div>
  );
}
