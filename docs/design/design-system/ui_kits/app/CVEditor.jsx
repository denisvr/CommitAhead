// Design-system primitives are read inside each component: this file is also swept
// into _ds_bundle.js, where module-top-level reads would resolve before the namespace is populated.

function Preview({ cv }) {
  const { Chip } = window.CommitAheadDesignSystem_80fdcb;
  return (
    <div style={{ border: '1px solid var(--border)', borderRadius: 'var(--radius-sm)', background: 'var(--surface)', padding: 'var(--space-16) var(--space-18)', fontSize: 'var(--text-sm)', lineHeight: 'var(--leading-prose)' }}>
      <div style={{ fontSize: 'var(--text-headline)', fontWeight: 'var(--weight-bold)', letterSpacing: 'var(--track-headline)' }}>Denis Silva</div>
      <div style={{ color: 'var(--text-muted)', marginTop: 2 }}>Senior Backend Engineer</div>
      <div style={{ fontFamily: 'var(--font-mono)', fontSize: 'var(--text-xs)', color: 'var(--text-faint)', marginTop: 'var(--space-4)' }}>
        {[cv.include.email && 'denis@example.com', cv.include.phone && '+44 7700 900000', cv.include.address && 'London, United Kingdom'].filter(Boolean).join(' · ')}
      </div>
      <p style={{ margin: 'var(--space-10) 0 0', color: 'var(--text-muted)' }}>{cv.summary}</p>
      <div style={{ marginTop: 'var(--space-14)', fontFamily: 'var(--font-mono)', fontWeight: 'var(--weight-medium)', fontSize: 'var(--text-micro)', letterSpacing: 'var(--track-label)', textTransform: 'uppercase', color: 'var(--text-faint)', paddingBottom: 'var(--space-3)', borderBottom: '1px solid var(--border-soft)' }}>Experience</div>
      {cv.experience.filter((e) => e.on).map((e) => (
        <div key={e.id} style={{ padding: 'var(--space-8) 0', borderBottom: '1px solid var(--border-soft)' }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', gap: 'var(--space-8)', alignItems: 'baseline', flexWrap: 'wrap' }}>
            <span style={{ fontWeight: 'var(--weight-semibold)', minWidth: 0 }}>{e.role}</span>
            <span style={{ fontFamily: 'var(--font-mono)', fontSize: 'var(--text-xs)', color: 'var(--text-faint)', whiteSpace: 'nowrap' }}>{e.dates}</span>
          </div>
          <div style={{ color: 'var(--text-muted)', fontSize: 'var(--text-xs)', margin: '2px 0 var(--space-4)' }}>{e.company}</div>
          <div style={{ color: 'var(--text-muted)' }}>{e.summary}</div>
        </div>
      ))}
      <div style={{ marginTop: 'var(--space-12)', display: 'flex', gap: 'var(--space-4)', flexWrap: 'wrap' }}>
        {cv.skills.map((s) => <Chip key={s}>{s}</Chip>)}
      </div>
    </div>
  );
}

function CVEditor() {
  const { PageHeader, Button, Tabs, Field, Input, Select, Checkbox, Textarea } = window.CommitAheadDesignSystem_80fdcb;
  const [cv, setCv] = React.useState(window.CA.cv);
  const [tab, setTab] = React.useState('content');
  const toggleInc = (k) => setCv({ ...cv, include: { ...cv.include, [k]: !cv.include[k] } });
  const toggleExp = (id) => setCv({ ...cv, experience: cv.experience.map((e) => e.id === id ? { ...e, on: !e.on } : e) });
  const included = cv.experience.filter((e) => e.on).length;

  return (
    <>
      <PageHeader
        kicker="CV presentation · 1 of 2"
        title={cv.label}
        summary={'Curated from your professional profile for ' + cv.market + '. ' + included + ' of ' + cv.experience.length + ' experience entries included, ' + cv.pageLimit + '-page limit.'}
        actions={<><Button size="sm" variant="secondary" icon="sparkles">Analyse with AI</Button><Button size="sm" icon="download">Export</Button></>}
      />
      <Tabs value={tab} onChange={setTab} style={{ marginBottom: 'var(--space-14)' }}
        items={[{ value: 'content', label: 'Content' }, { value: 'preview', label: 'Preview' }, { value: 'settings', label: 'Presentation settings' }]} />

      {tab === 'preview' ? <Preview cv={cv} /> : null}

      {tab === 'content' ? (
        <div style={{ display: 'grid', gridTemplateColumns: '1fr 320px', gap: 'var(--space-20)', alignItems: 'start' }}>
          <div>
            <Field label="Summary override" hint="Leave empty to use the profile summary.">
              <Textarea rows={4} value={cv.summary} onChange={(e) => setCv({ ...cv, summary: e.target.value })} />
            </Field>
            <div style={{ margin: 'var(--space-16) 0 var(--space-6)', fontFamily: 'var(--font-mono)', fontWeight: 'var(--weight-medium)', fontSize: 'var(--text-micro)', letterSpacing: 'var(--track-label)', textTransform: 'uppercase', color: 'var(--text-faint)' }}>Experience — select and order</div>
            {cv.experience.map((e) => (
              <div key={e.id} style={{ display: 'flex', gap: 'var(--space-6)', padding: 'var(--space-7) 0', borderBottom: '1px solid var(--border-soft)', opacity: e.on ? 1 : 0.55 }}>
                <Checkbox checked={e.on} onChange={() => toggleExp(e.id)} />
                <div>
                  <div style={{ display: 'flex', gap: 'var(--space-6)', alignItems: 'baseline' }}>
                    <span style={{ fontSize: 'var(--text-md)', fontWeight: 'var(--weight-medium)' }}>{e.role}</span>
                    <span style={{ fontFamily: 'var(--font-mono)', fontSize: 'var(--text-xs)', color: 'var(--text-faint)' }}>{e.dates}</span>
                  </div>
                  <div style={{ fontSize: 'var(--text-xs)', color: 'var(--text-muted)', marginTop: 2 }}>{e.company}</div>
                </div>
              </div>
            ))}
          </div>
          <aside style={{ borderLeft: '1px solid var(--border-soft)', paddingLeft: 'var(--space-14)' }}>
            <div style={{ marginBottom: 'var(--space-8)', fontFamily: 'var(--font-mono)', fontWeight: 'var(--weight-medium)', fontSize: 'var(--text-micro)', letterSpacing: 'var(--track-label)', textTransform: 'uppercase', color: 'var(--text-faint)' }}>Live preview</div>
            <div style={{ transform: 'scale(0.7)', transformOrigin: 'top left', width: '143%' }}><Preview cv={cv} /></div>
          </aside>
        </div>
      ) : null}

      {tab === 'settings' ? (
        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 'var(--space-12) var(--space-16)', maxWidth: 620 }}>
          <Field label="Label" htmlFor="lbl"><Input id="lbl" defaultValue={cv.label} /></Field>
          <Field label="Target market" htmlFor="mkt"><Input id="mkt" defaultValue={cv.market} /></Field>
          <Field label="Locale" htmlFor="loc"><Select id="loc" defaultValue={cv.locale} options={[{ value: 'en-GB', label: 'en-GB — United Kingdom' }, { value: 'en-US', label: 'en-US — United States' }, { value: 'pt-PT', label: 'pt-PT — Portugal' }]} /></Field>
          <Field label="Export template" htmlFor="tpl"><Select id="tpl" options={[{ value: 'rc', label: 'Reverse chronological' }, { value: 'sk', label: 'Skills first' }]} /></Field>
          <Field label="Page limit" htmlFor="pl"><Input id="pl" type="number" defaultValue={cv.pageLimit} /></Field>
          <Field label="Date format" htmlFor="df"><Select id="df" options={[{ value: 'my', label: 'March 2022' }, { value: 'sn', label: '03/2022' }]} /></Field>
          <div style={{ gridColumn: '1 / -1' }}>
            <div style={{ marginBottom: 'var(--space-6)', fontFamily: 'var(--font-mono)', fontWeight: 'var(--weight-medium)', fontSize: 'var(--text-micro)', letterSpacing: 'var(--track-label)', textTransform: 'uppercase', color: 'var(--text-faint)' }}>Personal details shown on this presentation</div>
            <div style={{ display: 'flex', gap: 'var(--space-14)', flexWrap: 'wrap' }}>
              <Checkbox checked={cv.include.photo} onChange={() => toggleInc('photo')} label="Photo" />
              <Checkbox checked={cv.include.email} onChange={() => toggleInc('email')} label="Email" />
              <Checkbox checked={cv.include.phone} onChange={() => toggleInc('phone')} label="Phone" />
              <Checkbox checked={cv.include.address} onChange={() => toggleInc('address')} label="Address" />
            </div>
            <p style={{ marginTop: 'var(--space-6)', fontSize: 'var(--text-xs)', color: 'var(--text-faint)', maxWidth: '58ch', lineHeight: 'var(--leading-prose)' }}>
              These control rendering only. Contact details always live on the professional profile and are never duplicated onto a presentation.
            </p>
          </div>
        </div>
      ) : null}
    </>
  );
}
window.CVEditor = CVEditor;
