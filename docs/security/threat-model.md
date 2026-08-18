# CommitAhead — Threat Model and Security Controls

## Assets

| Asset | Sensitivity |
|---|---|
| Professional profile, CV data, experience entries | High — personal career intelligence |
| Auth tokens (access token, refresh token) | High — authentication credentials |
| Supabase service-role key | High — full database access |
| Database and backups | High — contains all of the above |

---

## Trust Boundaries

| Boundary | Trusted side | Untrusted side |
|---|---|---|
| Authenticated session | Validated, enabled-user request, isolated to that user's own data (ADR-0015) | Unauthenticated request, or a request from a disabled/unknown user |
| Backend API | Validated + sanitised inputs | Browser input |
| Supabase PostgreSQL | Backend (least-privileged credential) | All direct client access (blocked by RLS) |
| Supabase Auth | Backend PKCE callback and server-held Supabase credentials | Browser and unauthenticated internet; no Supabase key is shipped to React |
| CI / deployment / secrets | Secrets store, GitHub Actions secrets | Code repository (secrets never committed) |

---

## Priority Threats

| Threat | Priority | Primary controls |
|---|---|---|
| Unauthorized access | High | Auth middleware; `sub` must match an existing, enabled application `User` (ADR-0015); 15-min JWT; HttpOnly cookies |
| Credential theft | High | HttpOnly + Secure + SameSite=Strict cookies; no tokens in localStorage; short token lifetime |
| XSS | High | CSP from `default-src 'none'`; restricted CommonMark; DOMPurify |
| Misconfiguration | Medium | RLS blocks all direct client access; Supabase anon key backend-only; secrets never in source |
| Deep DDoS | Low (deferred) | Rate limiting provides basic protection; dedicated DDoS mitigation is post-MVP |

---

## Controls by Category

### Authentication
- Supabase magic link with PKCE, completed by the backend callback
- Magic-link initiation: accepts only a provisioned, enabled user's email; Supabase is never called for any other email; response status/body is generic either way, preventing enumeration through those channels — the timing difference between calling Supabase or not is a separate, accepted residual risk, mitigated only by the rate limit on this endpoint
- PKCE state cookie: `SameSite=Lax` (required for email redirect); session cookies: `SameSite=Strict`
- Access tokens expire in 15 minutes; proactive/single-flight refresh before expiry
- Refresh cookie scoped to `/auth/refresh`; requires POST + CSRF validation; rotates atomically
- Absolute session timeout: 7 days
- Logout: revokes refresh token, clears cookies; 15-minute residual window accepted (no denylist in MVP)
- Public signup disabled in Supabase; every user's account is pre-provisioned out-of-band, not self-registered

### Authorisation
- Every request: middleware validates JWT (issuer, audience, signature, expiry, and that `sub` resolves to an existing, enabled application `User` — ADR-0015) and scopes all data access to that user's `OwnerUserId`
- All foreign ID references validated in use cases (404 for missing resources, 422 for invalid related IDs)
- DB enforces FK constraints as a second line of defence
- Supabase RLS: enabled on all tables; no `anon` or `authenticated` policies — direct Data API access is denied
- EF Core uses the dedicated `commitahead_app` PostgreSQL role with minimal grants and explicit RLS policies; a separate migration credential owns schema changes
- The service-role key is not an Npgsql credential and is reserved for backend Auth administration only
- CSRF validation required on all state-changing requests

### Transport and Data at Rest
- TLS required in production (HSTS with `max-age=31536000`; `includeSubDomains` only when all controlled subdomains are HTTPS)
- Database, logs, and backups encrypted at rest (Supabase defaults + operator responsibility)
- Periodic restoration tests to verify backup integrity

### Markdown / XSS
- Stored as raw Markdown; sanitised at every rendering boundary
- `react-markdown` without raw HTML
- Allowed link schemes: `https`, `http`, `mailto` — blocked: `javascript:`, `data:`
- No images, iframes, or embedded HTML in Markdown output
- CV/PDF export: same allowlist and sanitisation before HTML generation
- Every HTML-producing pipeline applies an allowlist sanitizer appropriate to its runtime; DOMPurify is used only when the selected browser/Node export pipeline supports it
- CSP as defence-in-depth (see Security Headers)

### Security Headers
```
Content-Security-Policy:
  default-src 'none';
  script-src 'self';
  style-src 'self';
  img-src 'self' blob:;
  font-src 'self';
  connect-src 'self';
  manifest-src 'self';
  object-src 'none';
  frame-ancestors 'none';
  base-uri 'self';
  form-action 'self';
Strict-Transport-Security: max-age=31536000 [; includeSubDomains — conditional]
X-Content-Type-Options: nosniff
X-Frame-Options: DENY
Referrer-Policy: no-referrer
Permissions-Policy: camera=(), microphone=(), geolocation=(), payment=()
Cache-Control: no-store  [on all authenticated and API responses]
```
CSP tested first in report-only mode; exceptions added only after verified violations.

### Secrets Management
- `.NET User Secrets` for local development
- Hosting/CI secrets store in deployed environments (provider TBD)
- Supabase anon key: backend-only (not shipped to browser)
- Supabase service-role key: backend-only; used only for Auth-session administration
- ASP.NET Data Protection key ring: persisted, encrypted; used for cookies and antiforgery
- Credentials rotated when exposed

### Logging Policy
**Never log**: headers, cookies, query strings, PKCE codes, request/response bodies, user-authored content.

**Production log fields**: correlation ID, route template, HTTP method, status code, duration, safe error code/type.

Log access is restricted. The exact production retention period remains TBD in `docs/tbd.md`.

### Dependency Security
- Direct and transitive dependency scanning: `dotnet list package --vulnerable` + `npm audit --audit-level=high`; high/critical vulnerabilities fail PRs
- Exceptions: documented, time-limited, reviewed
- Lock files committed; `--locked-mode` NuGet restore and `npm ci` in CI
- npm lifecycle scripts disabled where possible; required scripts explicitly reviewed
- Dependabot: NuGet, npm, Docker, GitHub Actions — no auto-merge
- GitHub Actions pinned to commit SHAs; workflow tokens at minimum permissions
- SBOM generated for production releases; final container image scanned with Trivy (high/critical blocks deployment)
- New direct dependencies reviewed for: maintenance status, ownership, repository health, necessity

### Automated Security Tests (every PR, blocking)
- Gitleaks secret scanning
- Dependency CVE scans (direct + transitive)
- Auth, enabled-user check, CSRF, CSP, CORS, `Cache-Control: no-store` API tests
- Markdown/XSS protocol tests
- Log-redaction tests
- NetArchTest architecture rules

### Post-merge / Release Security
- SBOM generation
- Trivy container scan (high/critical blocks deployment)
- OWASP ZAP baseline against test environment — fails on confirmed high-severity findings

### Pre-internet-deployment Checklist
A manual security checklist must be completed before the first internet-facing deployment. Penetration testing is deferred to post-MVP.
