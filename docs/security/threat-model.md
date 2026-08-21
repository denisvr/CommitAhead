# CommitAhead — Threat Model and Security Controls

**Security profile: S2 Standard** (ADR-0027) — targeting all applicable OWASP ASVS 5.0
Level 1 and Level 2 requirements. The evidence register at the end of this document is the
authority on which controls are evidenced today, which are owned outside this repository, and
which are still open.

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

## Evidence register (ADR-0027)

Required by the S2 profile. A control is only complete when its evidence is named, so this table
separates what is proven by an executable check in this repository from what depends on
infrastructure, an external platform setting, or a manual pass. "Owner" is where the control actually
lives, not who wrote the code.

Profile achievement is **not** claimed. This register tracks progress toward it, and the open items
below are part of the register, not footnotes to it.

### Evidenced in this repository

| Control | Owner | Implementation | Evidence | Status |
|---|---|---|---|---|
| Default-deny authorization | Code | `FallbackPolicy` and `DefaultPolicy` both require an authenticated, enabled user | `EnabledUserPolicyTests` | Pass |
| Explicit authorization on every operation | Code | `[Authorize]` / `[AllowAnonymous]` on every MVC action | `EndpointAuthorizationInventoryTests` — mechanical inventory, fails on a missing declaration | Pass |
| Approved anonymous inventory | Code | `ApprovedAnonymousEndpoints`, nine reviewed entries | Same test; also fails on a stale approval | Pass |
| Owner isolation (application) | Code | Every query and command scoped by `OwnerUserId` (ADR-0015) | Application use-case tests; `ProfessionalProfileRepositoryTests`, `CVPresentationRepositoryTests` | Pass |
| Owner isolation (database) | Code + configuration | RLS policies in `002_rls_users.sql` and `004_rls_phase2.sql`; least-privileged `commitahead_app` role | `RlsIsolationPhase2Tests` against a real provider | Pass at provider level only — see open items |
| Session confidentiality | Code | HttpOnly/Secure cookies; tokens never in browser storage | `RefreshEndpointTests`, `LogoutEndpointTests`, frontend `client.test.ts` | Pass |
| Access-token lifetime ceiling | Code | Server-side 15-minute `iat` check, independent of cookie MaxAge | `MeEndpointTests` | Pass |
| Antiforgery on unsafe methods | Code | `CsrfMiddleware` plus double-submit cookie and header | `CsrfTests` | Pass |
| Security headers and CSP | Code | `SecurityHeadersMiddleware`; no `unsafe-inline` for `style-src` | `SecurityHeadersTests` | Pass |
| CORS narrowness | Code | Dev-only named policy; no CORS in any other environment | `CorsTests` | Pass |
| Login abuse limit | Code | 5 per 15 minutes per remote address | `LoginEndpointTests` | Pass |
| State-changing request limit | Code | Global limiter, 120 per minute per authenticated subject; safe methods exempt | `RateLimitTests` — 429 enforced, and verified to fail when the limiter is removed | Pass |
| CV export abuse limit | Code | 10 per 5 minutes per authenticated subject | `RateLimitTests`, including per-caller partition isolation | Pass |
| JSON parser depth bound | Code | `MaxDepth = 32` on both serializer configurations | `TransportLimitTests` | Pass |
| Untrusted Markdown rendering | Code | `RestrictedMarkdownParser`; dangerous-protocol rejection | `RestrictedMarkdownParserTests`, `RestrictedMarkdown.test.tsx`, `restrictedUrlTransform` | Pass |
| Log redaction | Code | Exception type only, never message or object, on rollback failure | `RlsSessionContext` logging assertions; `RecordingLogger` use-case tests | Pass |
| Test authentication cannot reach production | Code | `E2EConfigurationGuard` fails closed; endpoint 404s outside the E2E environment; absent from OpenAPI | `E2EConfigurationGuardTests`, `E2ESessionEndpointTests` | Pass |
| Static security analysis | Build | `AnalysisLevelSecurity=latest-all`, warnings as errors | `dotnet build --warnaserror` in CI | Pass |
| Dependency vulnerabilities | Build | `dotnet list package --vulnerable --include-transitive`, `npm audit --audit-level=high`, committed lock files, `--locked-mode` restore | CI `backend` and `frontend` jobs | Pass |
| Secret scanning | Build | Gitleaks | CI `security` job | Pass |
| Supply-chain pinning | Build | Actions pinned to commit SHAs; `packageSourceMapping` pins `Devalente.Shared.*` to the private feed | `NuGet.Config`; workflow review | Pass |
| Private feed fails closed | Build | CI aborts with a named error when `DEVALENTE_PACKAGES_TOKEN` is absent, instead of restoring from a 401 | CI `backend` and `combined-artifact` jobs | Configured, not yet exercised — secret not created |
| Layer and dependency isolation | Code | NetArchTest rules | `ArchitectureTests` | Pass |

### Owned outside this repository

These cannot be proven by a test here. Each names what the user must configure.

| Control | Owner | Required action | Status |
|---|---|---|---|
| Private feed access, CI | GitHub repository settings | Add `DEVALENTE_PACKAGES_TOKEN`, a classic PAT with `read:packages` only, under Settings then Secrets and variables then **Actions** | Open |
| Private feed access, Dependabot | GitHub repository settings | Add the same value under Settings then Secrets and variables then **Dependabot**. It is a separate store; the Actions secret is not visible to it | Open |
| Package read grant | GitHub package settings | NuGet packages on GitHub Packages inherit permissions from the repository that published them. If the package page offers "Manage Actions access", grant `CommitAhead` read access and the workflows can use `GITHUB_TOKEN` instead of a PAT | Open |
| Branch protection, required checks, secret push protection | GitHub repository settings | Enable on `main`. Cannot be represented by files in this repository | Open |
| TLS termination, HSTS at the edge, private database networking | Infrastructure | Phase 6c; hosting undecided (`docs/tbd.md`) | Deferred |
| Data Protection key ring encryption at rest | Infrastructure | Open decision in `docs/tbd.md` | Deferred |
| Request body limit enforcement | Infrastructure and code | `TransportLimits.MaxRequestBodyBytes` is configured on Kestrel and asserted as configuration. A test host does not run Kestrel, so a 413 must be verified against a deployed instance, and the reverse proxy must not permit more | Open |
| Backup encryption, retention, restore testing | Operations | Open decision in `docs/tbd.md` | Deferred |
| Production log retention | Operations | Open decision in `docs/tbd.md` | Deferred |

### Open items required by S2

- **The API authorization matrix is incomplete.** Per-operation HTTP tests for another owner's
  resource, for malformed and wrong-audience credentials, and for insufficient permission do not
  exist yet. Owner isolation is currently proven at the provider level, which is necessary but not
  sufficient.
- **RLS is not proven through the HTTP pipeline.** `PostgresApiTestFactory` connects as the
  Testcontainers owner and never applies the RLS scripts, so no API test exercises the
  least-privileged `commitahead_app` role. ADR-0028 defines the tests that must close this.
- **Expected failures do not yet use the canonical Problem Details contract.** Rate-limit rejections
  return a plain-text body, and domain failures return bare status codes or a bespoke `outcomeCode`
  extension. Phase 7 of the adoption plan owns this.
- **ASVS requirement-level tailoring is not written down.** Only the profile is recorded. Password
  storage requirements are not applicable, because identity is delegated to Supabase and this
  application never receives a password, but the remaining Level 1 and Level 2 requirements have not
  been enumerated with versioned identifiers.
