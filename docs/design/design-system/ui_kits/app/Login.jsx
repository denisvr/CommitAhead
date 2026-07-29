// Design-system primitives are read inside each component: this file is also swept
// into _ds_bundle.js, where module-top-level reads would resolve before the namespace is populated.

function Login({ onSignIn }) {
  const { Brand, Field, Input, Button, Callout } = window.CommitAheadDesignSystem_80fdcb;
  const [sent, setSent] = React.useState(false);
  const [email, setEmail] = React.useState('denis@example.com');
  return (
    <div style={{ minHeight: '100%', display: 'flex', alignItems: 'center', justifyContent: 'center', background: 'var(--bg)', padding: 'var(--space-20)' }}>
      <div style={{ width: 340 }}>
        <Brand size={30} style={{ marginBottom: 'var(--space-8)' }} />
        <p style={{ margin: '0 0 var(--space-18)', fontSize: 'var(--text-lg)', lineHeight: 'var(--leading-prose)', color: 'var(--text-muted)' }}>
          Know what to study next, and why.
        </p>
        {sent ? (
          <>
            <Callout title="Check your inbox">
              A sign-in link is on its way to <strong style={{ color: 'var(--text)' }}>{email}</strong>. It is valid for 15 minutes and works once.
            </Callout>
            <div style={{ display: 'flex', gap: 'var(--space-4)', marginTop: 'var(--space-8)' }}>
              <Button onClick={onSignIn}>Continue</Button>
              <Button variant="ghost" onClick={() => setSent(false)}>Use a different address</Button>
            </div>
          </>
        ) : (
          <form onSubmit={(e) => { e.preventDefault(); setSent(true); }}>
            <Field label="Email" htmlFor="login-email">
              <Input id="login-email" type="email" value={email} onChange={(e) => setEmail(e.target.value)} autoComplete="email" />
            </Field>
            <Button type="submit" fullWidth style={{ marginTop: 'var(--space-8)' }}>Send sign-in link</Button>
          </form>
        )}
        <p style={{ marginTop: 'var(--space-14)', paddingTop: 'var(--space-8)', borderTop: '1px solid var(--border-soft)', fontSize: 'var(--text-xs)', color: 'var(--text-faint)', lineHeight: 'var(--leading-prose)' }}>
          Invite-only. Ask for access if you don't have one. There is no password to forget and no public sign-up.
        </p>
      </div>
    </div>
  );
}
window.Login = Login;
