import React from 'react';

const MARK = 'M2 0h28a2 2 0 0 1 2 2v44l-9.6-11.4h-4L0 46V2a2 2 0 0 1 2-2Z';
const SLOTS = 'M6 11h20v3H6z M6 18h13v3H6z';
const SLOT_SM = 'M6 11.5h20v3.5H6z';

/** Wordmark lockup for UI chrome: the outlined bookmark symbol + live type. */
export function Brand({ size = 17, symbol = true, style, ...rest }) {
  const h = Math.round(size * 0.82);
  const cuts = h >= 22 ? SLOTS : h >= 14 ? SLOT_SM : '';
  return (
    <span style={{ display: 'inline-flex', alignItems: 'center', gap: Math.round(size * 0.55), ...style }} {...rest}>
      {symbol ? (
        <svg viewBox="0 0 32 46" height={h} width={(h * 32) / 46} fill="var(--accent)" fillRule="evenodd" aria-hidden="true" style={{ display: 'block', flex: 'none' }}>
          <path d={MARK + ' ' + cuts} />
        </svg>
      ) : null}
      <span style={{ fontFamily: 'var(--font-sans)', fontWeight: 'var(--weight-bold)', fontSize: size, letterSpacing: 'var(--track-title)', color: 'var(--text)', lineHeight: 1 }}>
        Commit<span style={{ fontWeight: 'var(--weight-regular)', color: 'var(--accent)' }}>Ahead</span>
      </span>
    </span>
  );
}
