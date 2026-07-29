import React from 'react';
import { ScoreBreakdown } from './ScoreBreakdown.jsx';

export function QueueRow({ rank, title, meta, category, score, breakdown, dense, onClick, style, ...rest }) {
  const [hover, setHover] = React.useState(false);
  return (
    <div
      role={onClick ? 'button' : undefined}
      tabIndex={onClick ? 0 : undefined}
      onClick={onClick}
      onMouseEnter={() => setHover(true)}
      onMouseLeave={() => setHover(false)}
      style={{
        display: 'grid',
        gridTemplateColumns: dense ? '28px 1fr 120px 52px' : '28px 1fr 116px 52px',
        alignItems: dense ? 'center' : 'baseline',
        gap: dense ? 12 : 20,
        padding: dense ? '7px 10px' : '15px 10px',
        margin: '0 -10px',
        borderBottom: '1px solid var(--border-soft)',
        background: hover && onClick ? 'var(--surface-alt)' : 'transparent',
        cursor: onClick ? 'pointer' : 'default',
        transition: 'background-color var(--dur-fast) var(--ease-standard)',
        ...style,
      }}
      {...rest}
    >
      <span style={{ fontFamily: 'var(--font-mono)', fontSize: 'var(--text-xs)', color: 'var(--text-faint)', fontVariantNumeric: 'tabular-nums' }}>{rank}</span>
      <div style={{ minWidth: 0 }}>
        <div style={{ fontSize: dense ? 'var(--text-sm)' : 'var(--text-md)', fontWeight: 'var(--weight-medium)', color: 'var(--text)', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{title}</div>
        {!dense && meta ? <div style={{ fontSize: 'var(--text-xs)', color: 'var(--text-faint)', marginTop: 3 }}>{meta}</div> : null}
        {!dense && breakdown ? <div style={{ marginTop: 8 }}><ScoreBreakdown variant="bar" {...breakdown} /></div> : null}
      </div>
      <span style={{ fontFamily: 'var(--font-mono)', fontSize: 'var(--text-xs)', color: 'var(--text-muted)' }}>{category}</span>
      <span style={{ fontFamily: 'var(--font-mono)', fontSize: 'var(--text-md)', textAlign: 'right', fontVariantNumeric: 'tabular-nums', color: 'var(--text-muted)' }}>{score}</span>
    </div>
  );
}
