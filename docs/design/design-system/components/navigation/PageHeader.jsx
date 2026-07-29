import React from 'react';

export function PageHeader({ kicker, title, summary, actions, style, ...rest }) {
  return (
    <header style={{ marginBottom: 'var(--space-18)', ...style }} {...rest}>
      {kicker ? (
        <p style={{ margin: '0 0 var(--space-5)', fontFamily: 'var(--font-mono)', fontWeight: 'var(--weight-medium)', fontSize: 'var(--text-micro)', letterSpacing: 'var(--track-label)', textTransform: 'uppercase', color: 'var(--text-faint)' }}>{kicker}</p>
      ) : null}
      <div style={{ display: 'flex', alignItems: 'flex-start', justifyContent: 'space-between', gap: 'var(--space-8)' }}>
        <h1 style={{ margin: 0, fontFamily: 'var(--font-sans)', fontWeight: 'var(--weight-bold)', fontSize: 'var(--text-title)', lineHeight: 'var(--leading-title)', letterSpacing: 'var(--track-title)' }}>{title}</h1>
        {actions ? <div style={{ display: 'flex', gap: 'var(--space-4)', flex: 'none' }}>{actions}</div> : null}
      </div>
      {summary ? (
        <p style={{ margin: 'var(--space-3) 0 0', fontSize: 'var(--text-base)', color: 'var(--text-muted)', maxWidth: '58ch', textWrap: 'pretty' }}>{summary}</p>
      ) : null}
    </header>
  );
}
