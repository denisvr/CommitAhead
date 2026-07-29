import React from 'react';

const PARTS = [
  { key: 'importance', label: 'Importance', opacity: 1 },
  { key: 'demand', label: 'Demand', opacity: 0.62 },
  { key: 'masteryGap', label: 'Mastery gap', opacity: 0.32 },
];

export function ScoreBreakdown({ importance, demand, masteryGap, variant = 'rows', width = 104, style, ...rest }) {
  const vals = { importance, demand, masteryGap };
  const total = importance + demand + masteryGap;
  if (variant === 'bar') {
    return (
      <div
        role="img"
        aria-label={'Effective score ' + total + ': importance ' + importance + ', demand ' + demand + ', mastery gap ' + masteryGap}
        style={{ width, height: 4, display: 'flex', overflow: 'hidden', background: 'var(--border-soft)', ...style }}
        {...rest}
      >
        {PARTS.map((p) => (
          <i key={p.key} style={{ display: 'block', height: '100%', width: (vals[p.key] / 100) * width, background: 'var(--accent)', opacity: p.opacity }} />
        ))}
      </div>
    );
  }
  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-3)', ...style }} {...rest}>
      {PARTS.map((p) => (
        <span key={p.key} style={{ display: 'flex', alignItems: 'center', justifyContent: 'flex-end', gap: 'var(--space-4)', fontFamily: 'var(--font-mono)', fontSize: 'var(--text-micro)', color: 'var(--text-faint)', fontVariantNumeric: 'tabular-nums' }}>
          {p.label}
          <i style={{ display: 'block', height: 3, width: vals[p.key], background: 'var(--accent)', opacity: p.opacity }} />
          {vals[p.key]}
        </span>
      ))}
    </div>
  );
}
