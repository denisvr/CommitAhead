// Design-system primitives are read inside each component: this file is also swept
// into _ds_bundle.js, where module-top-level reads would resolve before the namespace is populated.

function Sparkline({ values }) {
  return (
    <div style={{ display: 'flex', alignItems: 'flex-end', gap: 5, height: 34 }}>
      {values.map((v, i) => (
        <div key={i} title={'Confidence ' + v + ' of 5'} style={{ width: 12, height: (v / 5) * 34, background: i === values.length - 1 ? 'var(--accent)' : 'var(--border)' }} />
      ))}
    </div>
  );
}

function Meta({ label, children }) {
  return (
    <div style={{ marginBottom: 'var(--space-10)' }}>
      <div style={{ fontFamily: 'var(--font-mono)', fontWeight: 'var(--weight-medium)', fontSize: 'var(--text-micro)', letterSpacing: 'var(--track-label)', textTransform: 'uppercase', color: 'var(--text-faint)', marginBottom: 'var(--space-3)' }}>{label}</div>
      <div style={{ fontSize: 'var(--text-md)', lineHeight: 'var(--leading-prose)', color: 'var(--text)' }}>{children}</div>
    </div>
  );
}

function StudyItemDetail({ itemId, onBack, reviewing }) {
  const { Button, Chip, Field, Textarea, RatingScale, Tabs, ScoreBreakdown, Dialog } = window.CommitAheadDesignSystem_80fdcb;
  const item = window.CA.queue.find((i) => i.id === itemId) || window.CA.queue[0];
  const [tab, setTab] = React.useState('details');
  const [rating, setRating] = React.useState(reviewing ? 4 : null);
  const [saved, setSaved] = React.useState(false);
  const [confirm, setConfirm] = React.useState(false);
  return (
    <>
      <Button variant="ghost" size="sm" icon="arrow-left" onClick={onBack} style={{ marginLeft: -12, marginBottom: 'var(--space-6)' }}>Back to queue</Button>
      <div style={{ display: 'flex', alignItems: 'flex-start', justifyContent: 'space-between', gap: 'var(--space-8)' }}>
        <div>
          <h1 style={{ margin: '0 0 var(--space-5)', fontSize: 'var(--text-title)', fontWeight: 'var(--weight-bold)', letterSpacing: 'var(--track-title)', lineHeight: 'var(--leading-title)' }}>{item.title}</h1>
          <div style={{ display: 'flex', gap: 'var(--space-4)' }}>
            <Chip>{item.category}</Chip>
            {item.difficulty ? <Chip>{item.difficulty}</Chip> : null}
            <Chip>{`Importance ${item.importance} of 5`}</Chip>
          </div>
        </div>
        <Button variant="danger" size="sm" icon="trash-2" onClick={() => setConfirm(true)}>Delete</Button>
      </div>

      <Tabs value={tab} onChange={setTab} style={{ margin: 'var(--space-14) 0 var(--space-14)' }}
        items={[{ value: 'details', label: 'Details' }, { value: 'reviews', label: 'Review history' }, { value: 'evidence', label: 'Evidence links' }]} />

      <div style={{ display: 'grid', gridTemplateColumns: '1fr 264px', gap: 'var(--space-20)', alignItems: 'start' }}>
        <div>
          {tab === 'details' ? (
            <>
              <Meta label="Patterns">{item.patterns}</Meta>
              <Meta label="Expected complexity"><span style={{ fontFamily: 'var(--font-mono)' }}>{item.complexity}</span></Meta>
              <Meta label="Approach">{item.approach}</Meta>
            </>
          ) : tab === 'reviews' ? (
            <div>
              {[['11 days ago', 3, 'Got the merge right but fumbled the touching-interval edge case.'],
                ['3 weeks ago', 2, 'Needed the hint. Sorting step was not obvious under time pressure.'],
                ['6 weeks ago', 3, null]].map(([when, score, note], i) => (
                <div key={i} style={{ padding: 'var(--space-7) 0', borderBottom: '1px solid var(--border-soft)' }}>
                  <div style={{ display: 'flex', justifyContent: 'space-between', gap: 'var(--space-8)' }}>
                    <span style={{ fontSize: 'var(--text-sm)', color: 'var(--text-muted)' }}>{when}</span>
                    <span style={{ fontFamily: 'var(--font-mono)', fontSize: 'var(--text-sm)' }}>{score} of 5</span>
                  </div>
                  {note ? <p style={{ margin: 'var(--space-3) 0 0', fontSize: 'var(--text-sm)', color: 'var(--text-muted)', lineHeight: 'var(--leading-prose)' }}>{note}</p> : null}
                </div>
              ))}
            </div>
          ) : (
            <div>
              {[['Ledgerline — Senior Backend Engineer', 'Job analysis', 4, 'Interval scheduling named under required algorithms.'],
                ['Northwind, technical round 2', 'Interview note', 3, 'Asked to merge overlapping booking windows.']].map(([t, kind, w, why], i) => (
                <div key={i} style={{ padding: 'var(--space-7) 0', borderBottom: '1px solid var(--border-soft)' }}>
                  <div style={{ display: 'flex', justifyContent: 'space-between', gap: 'var(--space-8)' }}>
                    <span style={{ fontSize: 'var(--text-md)', fontWeight: 'var(--weight-medium)' }}>{t}</span>
                    <span style={{ fontFamily: 'var(--font-mono)', fontSize: 'var(--text-xs)', color: 'var(--text-muted)', whiteSpace: 'nowrap' }}>weight {w} of 5</span>
                  </div>
                  <p style={{ margin: 'var(--space-3) 0 0', fontSize: 'var(--text-xs)', color: 'var(--text-faint)' }}>{kind} · {why}</p>
                </div>
              ))}
            </div>
          )}
        </div>

        <aside style={{ borderLeft: '1px solid var(--border-soft)', paddingLeft: 'var(--space-14)' }}>
          <div style={{ fontFamily: 'var(--font-mono)', fontWeight: 'var(--weight-medium)', fontSize: 'var(--text-micro)', letterSpacing: 'var(--track-label)', textTransform: 'uppercase', color: 'var(--text-faint)' }}>Mastery</div>
          <div style={{ fontFamily: 'var(--font-mono)', fontSize: 42, lineHeight: 1, margin: 'var(--space-5) 0 var(--space-3)', fontVariantNumeric: 'tabular-nums' }}>{item.mastery.toFixed(1)}</div>
          <p style={{ margin: '0 0 var(--space-8)', fontSize: 'var(--text-xs)', color: 'var(--text-faint)' }}>Average of your last three reviews</p>
          <Sparkline values={item.reviews || [3, 2, 3]} />

          <div style={{ margin: 'var(--space-14) 0 var(--space-5)', fontFamily: 'var(--font-mono)', fontWeight: 'var(--weight-medium)', fontSize: 'var(--text-micro)', letterSpacing: 'var(--track-label)', textTransform: 'uppercase', color: 'var(--text-faint)' }}>Effective score {item.score}</div>
          <ScoreBreakdown {...(item.breakdown || { importance: 30, demand: 20, masteryGap: 20 })} />

          <div style={{ marginTop: 'var(--space-16)', paddingTop: 'var(--space-12)', borderTop: '1px solid var(--border-soft)' }}>
            <Field label="Confidence after this session" hint="1 = could not start · 5 = could teach it">
              <RatingScale name="Confidence rating" value={rating} onChange={(v) => { setRating(v); setSaved(false); }} />
            </Field>
            <Textarea rows={3} placeholder="Optional notes" style={{ margin: 'var(--space-6) 0' }} />
            <Button fullWidth disabled={!rating} onClick={() => setSaved(true)}>{saved ? 'Review saved' : 'Save review'}</Button>
            {saved ? <p style={{ margin: 'var(--space-5) 0 0', fontSize: 'var(--text-xs)', color: 'var(--text-faint)' }}>Mastery and effective score recalculate on save.</p> : null}
          </div>
        </aside>
      </div>

      <Dialog open={confirm} destructive title="Delete this study item?" confirmLabel="Delete item"
        onCancel={() => setConfirm(false)} onConfirm={() => setConfirm(false)}>
        {item.title} has three reviews and two evidence links. Deleting it removes the review history and the demand those links contribute to other rankings.
      </Dialog>
    </>
  );
}
window.StudyItemDetail = StudyItemDetail;
