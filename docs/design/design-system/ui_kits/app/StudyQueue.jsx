// Design-system primitives are read inside each component: this file is also swept
// into _ds_bundle.js, where module-top-level reads would resolve before the namespace is populated.

function StudyQueue({ onOpenItem }) {
  const { PageHeader, Chip, Button, QueueRow, ScoreNumeral, ScoreBreakdown } = window.CommitAheadDesignSystem_80fdcb;
  const [filter, setFilter] = React.useState('All');
  const cats = ['All', 'Theory', 'LeetCode', 'System Design', 'Behavioral'];
  const all = window.CA.queue;
  const next = all[0];
  const rest = all.slice(1).filter((i) => filter === 'All' || i.category === filter);
  return (
    <>
      <PageHeader
        kicker="Tuesday · 18 active items"
        title="Study Queue"
        summary="Ranked by importance, evidence of demand, and how long ago you last proved you knew it."
        actions={<Button size="sm" icon="plus">New study item</Button>}
      />
      <div style={{ display: 'grid', gridTemplateColumns: '1fr 176px', gap: 'var(--space-18)', alignItems: 'start', paddingBottom: 'var(--space-14)', borderBottom: '1px solid var(--border-soft)' }}>
        <div>
          <p style={{ margin: '0 0 var(--space-5)', fontFamily: 'var(--font-mono)', fontWeight: 'var(--weight-medium)', fontSize: 'var(--text-micro)', letterSpacing: 'var(--track-label)', textTransform: 'uppercase', color: 'var(--accent)' }}>
            Next · {next.category}
          </p>
          <h2 style={{ margin: '0 0 var(--space-6)', fontSize: 'var(--text-headline)', fontWeight: 'var(--weight-semibold)', letterSpacing: 'var(--track-headline)', lineHeight: 1.25 }}>{next.title}</h2>
          <p style={{ margin: '0 0 var(--space-10)', fontSize: 'var(--text-md)', lineHeight: 'var(--leading-prose)', color: 'var(--text-muted)', maxWidth: '52ch', textWrap: 'pretty' }}>{next.why}</p>
          <div style={{ display: 'flex', gap: 'var(--space-5)' }}>
            <Button onClick={() => onOpenItem(next.id, true)}>Start review</Button>
            <Button variant="secondary" onClick={() => onOpenItem(next.id)}>Open item</Button>
          </div>
        </div>
        <div>
          <ScoreNumeral value={next.score} />
          <ScoreBreakdown {...next.breakdown} style={{ marginTop: 'var(--space-8)' }} />
        </div>
      </div>

      <div style={{ display: 'flex', alignItems: 'center', gap: 'var(--space-4)', margin: 'var(--space-14) 0 var(--space-4)', flexWrap: 'wrap' }}>
        <span style={{ fontFamily: 'var(--font-mono)', fontWeight: 'var(--weight-medium)', fontSize: 'var(--text-micro)', letterSpacing: 'var(--track-label)', textTransform: 'uppercase', color: 'var(--text-faint)', marginRight: 'var(--space-3)' }}>Then</span>
        {cats.map((c) => <Chip key={c} selected={c === filter} onClick={() => setFilter(c)}>{c}</Chip>)}
      </div>

      <div>
        {rest.map((i) => (
          <QueueRow key={i.id} rank={i.rank} title={i.title} category={i.category} score={i.score} meta={i.meta} onClick={() => onOpenItem(i.id)} />
        ))}
      </div>
      <p style={{ marginTop: 'var(--space-10)' }}>
        <Button variant="ghost" iconEnd="arrow-right">Show all 18 items</Button>
      </p>
    </>
  );
}
window.StudyQueue = StudyQueue;
