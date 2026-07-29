// Design-system primitives are read inside each component: this file is also swept
// into _ds_bundle.js, where module-top-level reads would resolve before the namespace is populated.

function JobAnalysis() {
  const { PageHeader, Button, Badge, DataTable, ProposalRow, Callout, Dialog } = window.CommitAheadDesignSystem_80fdcb;
  const job = window.CA.job;
  const b = window.CA.budget;
  const [decisions, setDecisions] = React.useState({});
  const [applied, setApplied] = React.useState(false);
  const [confirmRun, setConfirmRun] = React.useState(false);
  const pending = job.proposals.filter((p) => !decisions[p.id]).length;
  const accepted = job.proposals.filter((p) => decisions[p.id] === 'accepted').length;

  const rows = job.requirements.map((r) => ({
    id: String(r.id),
    priority: r.priority,
    text: r.text,
    match: r.match,
    gap: r.severity
      ? <Badge tone={r.severity === 'High' ? 'critical' : r.severity === 'Medium' ? 'caution' : 'good'}>{r.severity}</Badge>
      : <Badge tone="good" dot={false}>Matched</Badge>,
  }));

  return (
    <>
      <PageHeader
        kicker="Job analysis · added 2 days ago"
        title={job.title}
        summary="Six requirements extracted from the posting, each matched against your professional profile."
        actions={<Button size="sm" icon="sparkles" onClick={() => setConfirmRun(true)}>Analyse with AI</Button>}
      />

      <div style={{ display: 'flex', gap: 'var(--space-14)', marginBottom: 'var(--space-14)', fontFamily: 'var(--font-mono)', fontSize: 'var(--text-xs)', color: 'var(--text-faint)' }}>
        <span>{job.source}</span>
        <span>AI budget {b.used.toFixed(2)} of {b.cap.toFixed(2)} {b.currency} this month</span>
      </div>

      <DataTable
        columns={[
          { key: 'priority', label: 'Priority', width: '86px', muted: true },
          { key: 'text', label: 'Requirement' },
          { key: 'match', label: 'Match', width: '82px', muted: true },
          { key: 'gap', label: 'Gap', width: '104px' },
        ]}
        rows={rows}
      />

      <div style={{ marginTop: 'var(--space-20)' }}>
        <div style={{ display: 'flex', alignItems: 'baseline', justifyContent: 'space-between', gap: 'var(--space-8)', paddingBottom: 'var(--space-6)', borderBottom: '1px solid var(--border)' }}>
          <h2 style={{ margin: 0, fontSize: 'var(--text-lead)', fontWeight: 'var(--weight-semibold)', letterSpacing: 'var(--track-headline)' }}>Analysis draft</h2>
          <span style={{ fontFamily: 'var(--font-mono)', fontSize: 'var(--text-xs)', color: 'var(--text-faint)' }}>
            {applied ? 'Applied · ' + accepted + ' of 3 accepted' : pending + ' of 3 undecided'}
          </span>
        </div>

        {applied ? null : (
          <Callout title="Nothing changes until you apply this draft" style={{ margin: 'var(--space-8) 0 var(--space-6)' }}>
            Every proposal needs an explicit accept or reject. Accepted link proposals become evidence links, which is what raises demand in your queue.
          </Callout>
        )}

        {job.proposals.map((p) => (
          <ProposalRow
            key={p.id}
            kind={p.kind}
            rationale={p.rationale}
            status={decisions[p.id] || 'pending'}
            onAccept={() => setDecisions({ ...decisions, [p.id]: 'accepted' })}
            onReject={() => setDecisions({ ...decisions, [p.id]: 'rejected' })}
          >
            {p.text}
          </ProposalRow>
        ))}

        <div style={{ display: 'flex', gap: 'var(--space-5)', marginTop: 'var(--space-10)', alignItems: 'center' }}>
          <Button disabled={pending > 0 || applied} onClick={() => setApplied(true)}>
            {applied ? 'Draft applied' : 'Apply draft'}
          </Button>
          <Button variant="secondary" disabled={applied}>Discard draft</Button>
          {pending > 0 ? (
            <span style={{ fontSize: 'var(--text-xs)', color: 'var(--text-faint)' }}>Decide every proposal first — {pending} left.</span>
          ) : null}
        </div>
      </div>

      <Dialog open={confirmRun} title="Analyse this job posting?" confirmLabel="Run analysis"
        onCancel={() => setConfirmRun(false)} onConfirm={() => setConfirmRun(false)}>
        This sends the extracted posting text to the AI provider once and produces a new draft. Estimated cost 0.18 {b.currency}, charged against your monthly budget. It replaces any pending draft for this analysis.
      </Dialog>
    </>
  );
}
window.JobAnalysis = JobAnalysis;
