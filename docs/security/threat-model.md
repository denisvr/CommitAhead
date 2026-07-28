# CommitAhead — Threat Model and Security Controls

## Assets

| Asset | Sensitivity |
|---|---|
| Professional profile, CV data, experience entries | High — personal career intelligence |
| Job analyses, gap assessments, interview notes | High — confidential preparation intelligence |
| Behavioral stories, LeetCode solutions, study notes | Medium — personal but not externally sensitive |
| AI analysis drafts | Medium — derived from high-sensitivity inputs |
| Auth tokens (access token, refresh token) | High — authentication credentials |
| AI provider API key | High — cost exposure |
| Supabase service-role key | High — full database access |
| Database and backups | High — contains all of the above |
| AI budget (daily/monthly allowance) | Medium — financial exposure |

---

## Trust Boundaries

| Boundary | Trusted side | Untrusted side |
|---|---|---|
| Authenticated session | Validated owner request | Unauthenticated or non-owner request |
| Backend API | Validated + sanitised inputs | Browser, uploaded files, pasted text, AI output |
| Supabase PostgreSQL | Backend (least-privileged credential) | All direct client access (blocked by RLS) |
| Supabase Auth | Backend PKCE callback and server-held Supabase credentials | Browser and unauthenticated internet; no Supabase key is shipped to React |
| Supabase Storage | Backend (service-role key) | All direct client access |
| AI provider | Backend `IAIProvider` call | Frontend, domain layer |
| CI / deployment / secrets | Secrets store, GitHub Actions secrets | Code repository (secrets never committed) |

---

## Priority Threats

| Threat | Priority | Primary controls |
|---|---|---|
| Unauthorized access | High | Auth middleware; `sub == OWNER_USER_ID`; 15-min JWT; HttpOnly cookies |
| Credential theft | High | HttpOnly + Secure + SameSite=Strict cookies; no tokens in localStorage; short token lifetime |
| XSS / file parsing | High | CSP from `default-src 'none'`; restricted CommonMark; DOMPurify; PDF text-only parser with strict limits |
| Prompt injection / data disclosure | High | Source text marked untrusted; AI receives no tools/URLs/secrets; minimal inputs per command; structured output validated |
| AI cost abuse | High | Auth required; 10 calls/hour rate limit; one in-flight limit; idempotency key; daily + monthly budget with atomic reservation |
| Misconfiguration | Medium | RLS blocks all direct client access; Supabase anon key backend-only; secrets never in source |
| Deep DDoS | Low (deferred) | Rate limiting provides basic protection; dedicated DDoS mitigation is post-MVP |

---

## Controls by Category

### Authentication
- Supabase magic link with PKCE, completed by the backend callback
- Magic-link initiation: accepts only owner email, generic response, rate-limited
- PKCE state cookie: `SameSite=Lax` (required for email redirect); session cookies: `SameSite=Strict`
- Access tokens expire in 15 minutes; proactive/single-flight refresh before expiry
- Refresh cookie scoped to `/auth/refresh`; requires POST + CSRF validation; rotates atomically
- Absolute session timeout: 7 days
- Logout: revokes refresh token, clears cookies; 15-minute residual window accepted (no denylist in MVP)
- Public signup disabled in Supabase; owner account pre-created

### Authorisation
- Every request: middleware validates JWT (issuer, audience, signature, expiry, `sub == OWNER_USER_ID`)
- All foreign ID references validated in use cases (404 for missing resources, 422 for invalid related IDs)
- DB enforces FK constraints as a second line of defence
- Supabase RLS: enabled on all tables; no `anon` or `authenticated` policies — direct Data API access is denied
- EF Core uses the dedicated `commitahead_app` PostgreSQL role with minimal grants and explicit RLS policies; a separate migration credential owns schema changes
- The service-role key is not an Npgsql credential and is reserved for backend Auth/Storage administration only
- CSRF validation required on all state-changing requests

### Transport and Storage
- TLS required in production (HSTS with `max-age=31536000`; `includeSubDomains` only when all controlled subdomains are HTTPS)
- Database, Storage, logs, and backups encrypted at rest (Supabase defaults + operator responsibility)
- Periodic restoration tests to verify backup integrity

### PDF Upload
- Size limit: 5 MB
- Validation: extension, declared MIME, `%PDF-` magic bytes, page count, non-empty extracted text
- Rejected if: malformed, encrypted, image-only, wrong MIME, oversized
- Storage path: backend-generated quarantine key; original filename never used as path
- Parsing: text-only library; strict timeout, memory, page-count, and 50 000-character output limits
- Parsed once at upload; never re-executed
- Failed uploads: Storage object deleted immediately
- Never serve PDFs inline; never render scripts or follow embedded links; no parser network access
- Files whose extracted text exceeds the 50 000-character limit are rejected with an explicit user-visible error; extraction is never silently truncated

### Markdown / XSS
- Stored as raw Markdown; sanitised at every rendering boundary
- `react-markdown` without raw HTML
- Allowed link schemes: `https`, `http`, `mailto` — blocked: `javascript:`, `data:`
- No images, iframes, or embedded HTML in Markdown output
- CV/PDF export: same allowlist and sanitisation before HTML generation
- AI-generated Markdown content: same pipeline, no exceptions
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

### AI Security
- Source text explicitly marked as untrusted data in system prompt
- AI receives no tools, URLs, DB access, or secrets
- Per-command data minimisation (see ADR-0002)
- Structured output required; proposals validated against schemas, IDs, lengths, enums, weights, and domain invariants before draft creation
- Configurable input/output token limits; explicit rejection (not silent truncation) when exceeded
- Every proposal requires human confirmation before domain state changes
- Raw prompts and responses never logged

### AI Cost Controls
- Authentication required to trigger any AI command
- Global rate limit: 10 calls/hour (configurable); 429 + `Retry-After` when exceeded
- One AI call in flight globally at a time
- One Pending draft per source (natural deduplication guard)
- Idempotency key required; server deduplicates duplicate requests
- Daily and monthly budgets persisted; a Reserved AIUsageRecord atomically reserves maximum estimated cost before the provider call and transitions to Completed or Failed afterward
- No automatic retries; every retry is an explicit user action
- `AIUsageRecord`: unique idempotency key, command/source, provider/model/pricing version/currency, Reserved/Completed/Failed status, reserved and actual tokens/cost, timestamps, safe outcome code, and optional draft ID — never content

### Secrets Management
- `.NET User Secrets` for local development
- Hosting/CI secrets store in deployed environments (provider TBD)
- Supabase anon key: backend-only (not shipped to browser)
- Supabase service-role key: backend-only; used only for Auth/Storage admin
- AI provider key: backend-only; never logged or included in prompts
- `OWNER_USER_ID`: protected server configuration
- ASP.NET Data Protection key ring: persisted, encrypted; used for cookies and antiforgery
- Credentials rotated when exposed

### Logging Policy
**Never log**: headers, cookies, query strings, PKCE codes, uploaded file content, request/response bodies, user-authored content, AI prompts, AI responses.

**Production log fields**: correlation ID, route template, HTTP method, status code, duration, safe error code/type, AI usage metadata (no content).

The sole additional operational field is a backend-generated `storageObjectKey` on a `StorageCleanupFailed` event, required for manual orphan cleanup. Original filenames and file contents are never logged; a Storage key grants no access to the private bucket by itself.

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
- Auth, owner check, CSRF, CSP, CORS, `Cache-Control: no-store` API tests
- Malicious upload rejection tests
- Markdown/XSS protocol tests
- AI schema validation tests
- Idempotency deduplication tests
- Rate limit and budget enforcement tests
- Log-redaction tests
- NetArchTest architecture rules

### Post-merge / Release Security
- SBOM generation
- Trivy container scan (high/critical blocks deployment)
- OWASP ZAP baseline against test environment with `FakeAIProvider` — fails on confirmed high-severity findings

### Pre-internet-deployment Checklist
A manual security checklist must be completed before the first internet-facing deployment. Penetration testing is deferred to post-MVP.
