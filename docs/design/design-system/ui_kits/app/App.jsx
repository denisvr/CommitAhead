// Design-system primitives are read inside each component: this file is also swept
// into _ds_bundle.js, where module-top-level reads would resolve before the namespace is populated.

function ThemeToggle() {
  const { IconButton } = window.CommitAheadDesignSystem_80fdcb;
  const [dark, setDark] = React.useState(false);
  React.useEffect(() => { document.documentElement.setAttribute('data-theme', dark ? 'dark' : 'light'); }, [dark]);
  return (
    <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', padding: '0 var(--space-5)', borderTop: '1px solid var(--border-soft)', paddingTop: 'var(--space-6)' }}>
      <span style={{ fontFamily: 'var(--font-mono)', fontSize: 'var(--text-micro)', letterSpacing: 'var(--track-label)', textTransform: 'uppercase', color: 'var(--text-faint)' }}>
        {window.CA.budget.used.toFixed(2)} / {window.CA.budget.cap.toFixed(2)} {window.CA.budget.currency}
      </span>
      <IconButton size="sm" icon={dark ? 'sun' : 'moon'} label={dark ? 'Switch to light theme' : 'Switch to dark theme'} onClick={() => setDark(!dark)} />
    </div>
  );
}

function App() {
  const { SidebarNav, EmptyState, Button } = window.CommitAheadDesignSystem_80fdcb;
  const [signedIn, setSignedIn] = React.useState(false);
  const [screen, setScreen] = React.useState('queue');
  const [item, setItem] = React.useState(null);
  const [reviewing, setReviewing] = React.useState(false);

  if (!signedIn) return <Login onSignIn={() => setSignedIn(true)} />;

  const go = (id) => { setScreen(id); setItem(null); };
  let body;
  if (item) body = <StudyItemDetail itemId={item} reviewing={reviewing} onBack={() => { setItem(null); setReviewing(false); }} />;
  else if (screen === 'queue') body = <StudyQueue onOpenItem={(id, r) => { setItem(id); setReviewing(!!r); }} />;
  else if (screen === 'jobs') body = <JobAnalysis />;
  else if (screen === 'profile') body = <CVEditor />;
  else if (screen === 'items') body = <StudyQueue onOpenItem={(id, r) => { setItem(id); setReviewing(!!r); }} />;
  else body = (
    <EmptyState title={screen === 'notes' ? 'No interview notes yet' : 'Settings'}
      action={screen === 'notes' ? <Button icon="plus">New interview note</Button> : null}>
      {screen === 'notes'
        ? 'Record what was actually asked after each round. Notes become evidence, and evidence is what moves items up your queue.'
        : 'Scoring weights, theme, AI budget and account. Not part of this UI kit.'}
    </EmptyState>
  );

  return (
    <div style={{ display: 'flex', minHeight: '100vh', background: 'var(--bg)' }}>
      <SidebarNav active={item ? 'queue' : screen} onNavigate={go} footer={<ThemeToggle />} />
      <main style={{ flex: 1, background: 'var(--surface)', borderLeft: '1px solid var(--border-soft)', padding: 'var(--page-pad-y) var(--page-pad-x) var(--space-32)', minWidth: 0 }}>
        <div style={{ maxWidth: 'var(--content-max)' }}>{body}</div>
      </main>
    </div>
  );
}
// Mounted from an inline block in index.html — inline scripts are not swept into the bundle.
window.CommitAheadApp = App;
