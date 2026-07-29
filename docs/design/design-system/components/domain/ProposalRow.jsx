import React from 'react';
import { Button } from '../core/Button.jsx';
import { Badge } from '../core/Badge.jsx';

export function ProposalRow({ kind, children, rationale, status = 'pending', onAccept, onReject, style, ...rest }) {
  return (
    <div
      style={{
        display: 'flex', alignItems: 'flex-start', justifyContent: 'space-between', gap: 'var(--space-8)',
        padding: 'var(--space-7) 0', borderBottom: '1px solid var(--border-soft)', ...style,
      }}
      {...rest}
    >
      <div style={{ minWidth: 0 }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 'var(--space-4)', marginBottom: 'var(--space-3)' }}>
          <span style={{ fontFamily: 'var(--font-mono)', fontWeight: 'var(--weight-medium)', fontSize: 'var(--text-micro)', letterSpacing: 'var(--track-label)', textTransform: 'uppercase', color: 'var(--text-faint)' }}>{kind}</span>
          {status !== 'pending' ? <Badge tone={status === 'accepted' ? 'good' : 'neutral'} dot={false}>{status === 'accepted' ? 'Accepted' : 'Rejected'}</Badge> : null}
        </div>
        <div style={{ fontSize: 'var(--text-md)', color: 'var(--text)' }}>{children}</div>
        {rationale ? <div style={{ fontSize: 'var(--text-xs)', color: 'var(--text-muted)', marginTop: 'var(--space-3)', lineHeight: 'var(--leading-prose)', maxWidth: '62ch' }}>{rationale}</div> : null}
      </div>
      {status === 'pending' ? (
        <div style={{ display: 'flex', gap: 'var(--space-4)', flex: 'none' }}>
          <Button size="sm" icon="check" onClick={onAccept}>Accept</Button>
          <Button size="sm" variant="secondary" icon="x" onClick={onReject}>Reject</Button>
        </div>
      ) : null}
    </div>
  );
}
