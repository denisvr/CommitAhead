import React from 'react';

/** 1–5 control used for Importance, InitialMastery and StudyReview confidence. */
export function RatingScale({ value, onChange, name = 'rating', min = 1, max = 5, disabled, style, ...rest }) {
  const items = [];
  for (let i = min; i <= max; i++) items.push(i);
  return (
    <div role="radiogroup" aria-label={name} style={{ display: 'flex', gap: 'var(--space-3)', ...style }} {...rest}>
      {items.map((i) => {
        const on = value === i;
        return (
          <button
            key={i}
            role="radio"
            aria-checked={on}
            disabled={disabled}
            onClick={() => onChange && onChange(i)}
            style={{
              width: 40, height: 40, borderRadius: 'var(--radius-sm)',
              fontFamily: 'var(--font-mono)', fontSize: 'var(--text-sm)',
              border: '1px solid ' + (on ? 'var(--accent)' : 'var(--border-strong)'),
              background: on ? 'var(--accent)' : 'var(--surface)',
              color: on ? 'var(--accent-contrast)' : 'var(--text-muted)',
              cursor: disabled ? 'not-allowed' : 'pointer',
              transition: 'background-color var(--dur-fast) var(--ease-standard), border-color var(--dur-fast) var(--ease-standard)',
            }}
          >
            {i}
          </button>
        );
      })}
    </div>
  );
}
