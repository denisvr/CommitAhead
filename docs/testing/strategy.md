# CommitAhead — Testing Strategy

## Tooling

| Layer | Tools |
|---|---|
| Domain unit | xUnit, built-in assertions |
| Use-case | xUnit, handwritten repository fakes, `FakeAIProvider` |
| Repository / integration | xUnit, Testcontainers.PostgreSql, Respawn (serial execution) |
| API | xUnit, WebApplicationFactory, shared Testcontainers DB, `FakeAIProvider` |
| Architecture | NetArchTest |
| Frontend component | Vitest, React Testing Library, MSW |
| E2E | Playwright, Chromium only (planned — not implemented; Layer 7 is the normative contract) |
| AI adapter | xUnit, stubbed HTTP/SDK responses |

**Absolute rule**: zero real AI calls in any automated test. `FakeAIProvider` in all automated contexts.

---

## Layer 1: Domain Unit Tests

**What**: Pure domain logic — no DB, no HTTP, no I/O.

**Coverage:**
- EffectiveScore formula (representative boundaries: min=8, max=100, override=0, override=100)
- Demand clamping: `min(Σ weights, 5)`
- Mastery derivation: `initialMastery` before first review; average of up to 3 most recent ratings
- StudyItem deletion guard (blocked when reviews exist — EvidenceLink check is integration)
- Tag normalisation: trim → lowercase → kebab-case; deduplication
- `AnalysisDraft` status transitions: `Pending → Applied`, `Pending → Discarded`; re-applying non-Pending throws
- Apply decision-set validation: every proposal represented exactly once; duplicates/omissions rejected; accepted actionable proposals require complete final payloads; accepted StudyItemProposal requires user-selected InitialMastery
- Proposal statuses become Accepted/Rejected during Apply and cannot change afterward
- `PriorityOverride` validation: score ∈ [0,100], reason non-empty
- `StudyReview` confidence rating bounds: ∈ [1,5]
- `ScoringConfig` validation: all weights non-negative, sum = 100
- `YearMonth` ordering and equality
- `JobGap` invariant: no gap for a fully matched requirement
- `InterviewNote.otherLabel` required when `round = Other`
- Typed detail invariants (e.g. `LeetCodeDetails` problem number > 0 when present)

**Not tested here**: persistence, HTTP, AI calls, use case orchestration, EF Core mappings.

---

## Layer 2: Application Use-Case Tests

**What**: Non-trivial orchestration with handwritten fakes. No real DB; no real AI.

**Coverage:**
- `CreateStudyItem`, `SubmitStudyReview`, `ArchiveStudyItem`
- `ApplyAnalysisDraft`: original payloads preserved, accepted payloads and complete decisions persisted; accepted LinkProposals → EvidenceLinks; accepted StudyItemProposals → StudyItems; rejected proposals remain; omissions/duplicates and applying non-Pending throw
- `AnalyzeJobAnalysis` via `FakeAIProvider` (success scenario): draft created with correct proposals; source entity not mutated
- `AnalyzeJobAnalysis` via `FakeAIProvider` (failure scenarios): timeout, provider failure, malformed proposals, duplicates, empty output
- One-Pending-draft-per-source guard: attempting a second analysis while a Pending draft exists is rejected
- CVPresentation reference validation: selectedExperienceIds must reference valid canonical entries
- AnalyzeCVPresentation resolves only selected canonical content and the compact StudyItem catalogue; AnalyzeInterviewNote also receives the compact catalogue
- `UpdateScoringConfig`: weights validated before persisting
- `SetPriorityOverride` / `ClearPriorityOverride`
- `CreateJobAnalysisFromUpload`: happy path; empty/oversized/wrong-MIME/wrong-filename/wrong-magic-bytes content rejected before any Storage call; an extraction or Storage failure triggers cleanup with the exact known key and either a safe validation error or the original infrastructure exception, depending on which failed; a genuine caller cancellation still runs cleanup but propagates unchanged; both the fake Storage client and the fake extractor receive the exact same complete bytes
- `DeleteJobAnalysis`: an `UploadedFile` source's Storage object is deleted after the DB delete commits; a `PastedText` source never calls Storage; a Storage-delete failure still reports success (the DB row is already gone)

**Not tested here**: DB constraints, cascade behaviour, transaction atomicity (→ integration tests).

---

## Layer 3: Repository / Integration Tests

**What**: Real PostgreSQL via Testcontainers. Migrations applied once per session; Respawn resets data between tests. Tests run **serially** against the shared container.

**Coverage:**
- EF Core mapping round-trips for all aggregates (write → read → assert equality)
- `JobSource` discriminated union round-trips: PastedText and UploadedFile
- Typed detail variants round-trips: all four categories
- DB unique constraint on `(source_type, source_id, target_study_item_id)` for `EvidenceLink`
- Partial unique index: one-Pending-draft-per-source
- Non-cascade FK: StudyItem delete blocked when EvidenceLinks exist
- Cascade (application-managed): source deletion → EvidenceLinks plus AnalysisDraft/proposal children deleted atomically; content-free AIUsageRecords retained
- JobAnalysis deletion sets optional `InterviewNote.jobAnalysisId` references to null without deleting InterviewNotes
- `ApplyAnalysisDraft` atomicity: partial failure rolls back entirely
- CVPresentation ordered selection tables: FK enforcement, same-presentation position uniqueness, order round-trip, application rejection of entries from another profile, and canonical-entry deletion removing only affected selections
- ProfessionalProfile skill references: Experience/Project may reference only Skills in the same profile; referenced Skill deletion is blocked
- Ranked-list query: correct ordering with mixed override and computed scores
- `ScoringConfig` resolve: override row used when present; code defaults when absent
- Mastery derivation in query: initialMastery before first review; avg of up to 3 most recent
- AIUsageRecord: unique idempotency key, atomic Reserved insertion, daily/monthly budget calculation, Completed reconciliation, Failed release, lazy expiration of stale reservations, and replay returning the existing draft
- **RLS/runtime-role isolation** (Phase 1, `RlsIsolationTests`): a dedicated fixture bootstraps its own Testcontainers instance the same way `setup-local-db.ps1` bootstraps a real one (roles → migrations → RLS scripts) and connects as the real, least-privileged `commitahead_app` role — never the Testcontainers-owner connection every other test in this layer uses. Proves: an owner can CRUD their own StudyItems; cannot read or mutate another owner's rows even via a raw `UPDATE` with no `WHERE owner_user_id` clause; a connection with no owner context set sees zero business rows; the runtime role cannot perform DDL; the setup scripts remain safe when applied a second time.
- **RLS/runtime-role isolation** (Phase 2, `RlsIsolationPhase2Tests`): the same bootstrap and proof shape, extended through `004_rls_phase2.sql`, against `professional_profiles`/`cv_presentations` (owner-scoped directly) and a representative transitively-scoped child table (`skills`, via `professional_profile_id`).
- **RLS/runtime-role isolation** (Phase 3, `RlsIsolationPhase3Tests`): the same bootstrap and proof shape, extended through `005_rls_phase3.sql`, against `job_analyses`/`interview_notes` (owner-scoped directly) and a representative transitively-scoped child table (`job_requirements`, via `job_analysis_id`).
- **RLS/runtime-role isolation** (Phase 4, `RlsIsolationPhase4Tests`): the same bootstrap and proof shape, extended through `007_rls_phase4.sql`, against `analysis_drafts`/`ai_usage_records` (owner-scoped directly) and a representative transitively-scoped child table (`link_proposals`, via `analysis_draft_id`).
- **JobAnalysis-upload adapters** (`PdfPigTextExtractorTests`, `SupabaseStorageClientTests`): the real PdfPig library against hand-crafted minimal PDF fixtures (never authored with PdfPig itself), and the real Storage HTTP client against a stubbed handler (never a live Supabase call) — see "PDF and CV Verification" below for exactly what each proves.

---

## Layer 4: API Tests

**What**: Full ASP.NET Core pipeline via WebApplicationFactory with shared Testcontainers PostgreSQL and `FakeAIProvider`. State verified through HTTP responses, not DbContext.

**Coverage:**
- Routing and serialisation for representative happy and error paths per controller
- Malformed JSON / missing required fields caught by automatic `[ApiController]` model binding → 400
- Semantic/domain validation (out-of-range values, invalid enums, invariant violations — thrown as `ArgumentException` and mapped centrally by `DomainValidationExceptionFilter`) → 422
- Missing resources → 404; invalid related IDs → 422; conflict (e.g. duplicate link) → 409
- **Auth**: unauthenticated → 401; unknown/disabled-user JWT → 403 (fallback authorization policy, ADR-0015) — never blocks the `[AllowAnonymous]` auth endpoints themselves
- Dedicated locally-signed JWT tests for token validation (issuer, audience, signature, expiry, sub)
- **CSRF**: state-changing requests without token → 400/403
- **Security headers**: CSP, `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, `Cache-Control: no-store` on API responses
- **CORS**: unapproved preflights denied and no response grants `Access-Control-Allow-Origin` to another origin; state-changing requests remain CSRF-protected
- **Malicious uploads**: invalid PDF, encrypted PDF, image-only PDF, wrong MIME, oversized → 422 with error
- **Markdown storage boundary**: Markdown is accepted as raw text within length limits and returned as JSON without server-side HTML rendering
- AI schema validation: malformed `FakeAIProvider` scenario → draft not created; error returned
- Idempotency: duplicate AI command with same key → same draft returned, not duplicated
- Rate limit: 11th AI call within the window → 429 with `Retry-After`
- Budget enforcement: call that would exceed daily/monthly limit → 429 with safe error code `AI_BUDGET_EXCEEDED`
- Log redaction: assert sensitive fields (tokens, cookies, bodies) absent from structured logs
- OpenAPI contract drift: regenerate TypeScript client + compile — compilation failure = contract broken

---

## Layer 5: Architecture Tests (NetArchTest)

Five assembly-level rules (see `CLAUDE.md` for full list).

---

## Layer 6: Frontend Component Tests

Vitest + React Testing Library + MSW cover:

- Typed StudyItem forms and validation
- AnalysisDraft review, complete proposal decisions, editable accepted payloads, and Apply submission
- SystemDesign reference solution reveal (transient UI state)
- CVPresentation editing and ordered selections
- JobAnalysis pasted-text and upload flows
- Restricted Markdown rendering: embedded HTML is escaped/ignored; `javascript:` and `data:` links, images, and iframes never reach the DOM
- Production design primitives: keyboard interaction, visible focus behaviour, accessible names,
  disabled/loading states, and representative mobile/desktop layouts
- Frontend source guard: production components use CSS Modules/tokens and do not introduce inline
  style attributes or runtime-injected design markup

Score, Demand, and Mastery are rendered from API responses and are never recomputed in React. MSW provides representative success, loading, validation, unauthorised, and server-error variants per flow.

---

## FakeAIProvider Scenarios

Six deterministic fixture responses, one set per AI command:

| Scenario | Description |
|---|---|
| `Success` | Realistic proposals: 2 LinkProposals, 1 StudyItemProposal, 1 StructuredSuggestion, 1 AdvisorySuggestion |
| `EmptyOutput` | Provider returns valid response with zero proposals |
| `MalformedProposals` | Invalid IDs, out-of-range weights, missing required fields |
| `Duplicates` | Same source–target link proposed twice in one response |
| `Timeout` | Provider call times out after configured limit |
| `ProviderFailure` | Provider returns a 5xx error |

---

## AI Adapter Tests

The real adapter (`ProviderAIAdapter`, renamed after provider selection) is tested with stubbed HTTP/SDK responses:
- Request construction: correct model, token limits, system/user message separation
- Response deserialisation: all proposal types correctly mapped
- Error mapping: 429 → rate limit error; 5xx → provider failure; timeout → timeout error
- Token limit enforcement: oversized inputs are rejected before calling the provider; no silent truncation

---

## PDF and CV Verification

**PDF ingestion (runs on every PR):**
- Normalised text extraction from a hand-crafted minimal valid PDF fixture (`PdfPigTextExtractorTests`, `JobAnalysesEndpointTests`)
- Malformed/non-PDF bytes, image-only PDF (no extractable text), too many pages (>20), extracted text exceeding the 50,000-character cap, and words from adjacent pages never merging at the page boundary → each proven explicitly against the *real* PdfPig extractor, not truncated (`PdfPigTextExtractorTests`)
- An encrypted PDF — a small, real, password-protected PDF (RC4-128, generated once with the independent `pypdf` library) committed as a binary fixture (`JobAnalyses/Fixtures/encrypted.pdf`, embedded into `CommitAhead.Infrastructure.Tests`) — → PdfPig, opened without that password, fails authentication and throws for real (`PdfPigTextExtractorTests`); not a claim resting on the source's catch clause alone
- Wrong declared MIME type, non-`.pdf` filename, missing `%PDF-` magic bytes, empty file, and content over 5 MB (enforced by actually counting bytes while copying, never by a trusted `Content-Length`) → 422, before any Storage call (`CreateJobAnalysisFromUploadUseCaseTests`, `JobAnalysesEndpointTests`)
- A Storage upload/extraction failure after a successful upload triggers a best-effort delete of the exact known quarantine key before the rejection is reported, and either kind of Storage cleanup failure (this one, or the one after a JobAnalysis deletion) logs the orphaned object's key — never the exception itself — for manual remediation (`CreateJobAnalysisFromUploadUseCaseTests`, `DeleteJobAnalysisUseCaseTests`)
- A JobGap referencing a nonexistent requirement, or a requirement that belongs to a different JobAnalysis, is rejected by a real PostgreSQL composite foreign key even when the in-memory invariant is bypassed directly — defense-in-depth, not just application-level validation (`JobAnalysisRepositoryTests`)
- **Not covered by a fixture-driven test**: a genuine parser timeout — there is no deterministic way to force PdfPig (synchronous, uncancellable) past its 10-second best-effort budget without a pathological file; the use case's own handling of a `TimedOut` failure is covered via a fake extractor instead, which proves nothing about PdfPig's real timing. The 10-second budget itself is best-effort only: PdfPig has no cancellable API, so a slow parse can keep running on its own thread after the budget elapses or the caller cancels — container memory/CPU limits are the actual backstop, not an in-process guarantee.

**CV export (runs on every PR — parsed content assertions):**
- Required text present (name, role, key entries)
- Entry ordering matches `selectedExperienceIds` order
- Excluded entries absent from output
- Locale date formatting correct (e.g. `en-GB` vs `de-DE`)
- Configured page limit respected

**Visual regression (post-merge or manual):**
- One deterministic snapshot per CV template in a fixed-font/container environment

---

## Layer 7: E2E Tests (Playwright — post-merge or manual)

**Not implemented yet** — no Playwright project, no `e2e/` code, and no E2E Docker stack exist in
this repo. Everything below is the **normative contract** that implementation must satisfy, not a
description of anything running today. `e2e/README.md` is the operational runbook for the same
contract; this document owns the *rules*, that one owns the *commands*. Both must be read before
changing E2E code.

Research basis: Playwright's official documentation, current release `1.62.x`, reviewed
2026-08-12. Where this contract departs from official guidance, the deviation is stated and
justified — see "Sources and project decisions" at the end of this layer.

### 7.1 Exactly four journeys

Four journeys, no more. Each maps to a stated MVP completion criterion
(`docs/product/brief.md`); a proposed fifth journey is a request to change that list, not an
addition to it. Because the runner is pinned to `workers: 1` (§7.7), Playwright executes spec files
in alphabetical order, so the numeric filename prefixes below are load-bearing and must be kept.

| # | File | Journey | MVP criterion it proves |
|---|---|---|---|
| 1 | `001-authenticated-access.spec.ts` | An unauthenticated visitor gets the login screen and cannot reach protected content; an authenticated session renders the app shell and `GET /api/me`; logout ends the session | Security controls in place |
| 2 | `002-study-queue-ranking.spec.ts` | Create a StudyItem → submit a StudyReview → the study queue reflects the new ranking | The study queue ranks items correctly |
| 3 | `003-job-analysis-draft.spec.ts` | Create a pasted-text JobAnalysis → Analyze → review the draft → accept some proposals and reject others → Apply → the accepted effects are visible on the source | AI commands produce valid AnalysisDrafts and apply accepted proposals |
| 4 | `004-cv-presentation-export.spec.ts` | Edit a CVPresentation's selections → export → a PDF is downloaded | At least one CVPresentation can be edited and exported |

Journey 3 uses a **pasted-text** JobAnalysis, never a PDF upload. Upload goes through Supabase
Storage, which §7.6 forbids; the upload path is already covered end-to-end by Layer 3/4 tests
against the real extractor and a stubbed Storage client.

### 7.2 The E2E environment is a separate, disposable stack

E2E runs against a real production-shaped stack: the production container image (built React SPA
served by real Kestrel) plus a real PostgreSQL with real EF migrations and all real RLS scripts
applied. Not `WebApplicationFactory`, not Testcontainers, not `vite dev`.

That stack is **completely isolated** from both existing stacks. Every axis must differ — this is
the primary safeguard against an E2E run touching real data:

| Axis | Dev (`backend/docker-compose.yml`) | Local production-like (`docker-compose.prod.yml`) | **E2E (`docker-compose.e2e.yml`)** |
|---|---|---|---|
| Compose project | (default, directory-derived) | `commitahead-prod` | **`commitahead-e2e`** |
| Database name | `commitahead` | `commitahead` | **`commitahead_e2e`** |
| DB host port | `5433` | `127.0.0.1:5434` | **`127.0.0.1:5435`** |
| App host port | n/a | `127.0.0.1:8080` | **`127.0.0.1:8081`** |
| DB volume | named, persistent | named, persistent | **none — anonymous/`tmpfs`, destroyed with the container** |
| Data Protection keys | n/a | named volume | **ephemeral** |
| `ASPNETCORE_ENVIRONMENT` | `Development` | `Docker` | **`E2E`** |

Rules:

- **The E2E database must never be persistent.** No named volume. A stack that cannot outlive
  `docker compose down` cannot accumulate real data and cannot be mistaken for a real one.
- **A distinct database name (`commitahead_e2e`) is mandatory**, so that even a misconfigured
  connection string cannot silently land on the `commitahead` database of either other stack.
- All E2E ports bind to `127.0.0.1` only, consistent with ADR-0021.
- Anything that resets or seeds data must address the stack explicitly by both
  `-f docker-compose.e2e.yml` and `-p commitahead-e2e`. A reset helper that relies on ambient
  Docker context is a defect.
- `baseURL` is `http://localhost:8081`. The Playwright config must **fail fast** if `baseURL`
  resolves to `8080`, or if any DB-facing helper resolves to port `5433`/`5434` or database
  `commitahead` — a wrong-target run must be impossible, not merely discouraged.

Playwright's `webServer` option is deliberately **not** used to bring the stack up. The stack's
lifecycle is owned by explicit scripts because (a) the reset fixture must talk to the same Compose
project, and (b) `webServer` tears its process down by killing the process group, which would leave
Compose resources behind rather than running `down`. Startup readiness is the container's own
health check plus `GET /api/health`.

### 7.3 E2E-only authentication that cannot exist anywhere else

Real login is a Supabase magic link — externally delivered, non-deterministic, and a real Supabase
call, so §7.6 forbids it. Playwright's normal advice (sign in through the UI once in a `setup`
project, save `storageState`) therefore cannot be followed literally.

The contract:

- Authentication is obtained through an **E2E-only session hook in the API**, gated on
  `ASPNETCORE_ENVIRONMENT=E2E` **and** an explicit signing-key configuration value. It mints a
  locally-signed JWT for the seeded E2E user and writes the same auth cookies the real callback
  writes, so everything downstream of login is exercised unchanged. This mirrors the existing
  `AuthTestWebApplicationFactory` precedent, which already points JWT validation at a fixed local
  key instead of Supabase's JWKS — but as configuration, since a real container cannot be
  reconfigured in-process.
- **Fail closed at startup.** If the E2E signing key is configured while the environment is
  anything other than `E2E`, the application must throw during startup rather than start with the
  hook inert. "Inert unless enabled" is not sufficient; the presence of the key in a non-E2E
  environment is itself a misconfiguration and must be loud.
- The hook must be covered by an API test asserting it is unreachable under `Development`,
  `Docker`, and `Production`.
- The E2E signing key is a test-fixture value with no production meaning. It must never be reused
  as, or derived from, any real secret.

**`storageState` handling** — Playwright's docs are explicit that a saved browser-state file holds
cookies that can impersonate the account, and must be gitignored, never committed. For CommitAhead
that file holds a genuine (if E2E-only) session cookie, so:

- Saved state lives under `e2e/.auth/` and **must be gitignored before the first run**.
- Prefer writing it under the project's `outputDir`, which Playwright clears before every run.

**A CommitAhead-specific expiry constraint** the official pattern does not anticipate:
`AuthenticationServiceCollectionExtensions` enforces a 15-minute effective access-token lifetime
server-side against the token's `iat` claim, independently of cookie lifetime. A single
`storageState` minted once by one global `setup` project therefore **goes stale part-way through a
suite** and would produce spurious 401s in whichever journey happens to run last. The session must
be re-minted per journey file rather than once per run. This is a deliberate deviation from the
single-setup-project model, forced by our own session hardening.

Journey 1's unauthenticated half uses the documented empty-state form
(`test.use({ storageState: { cookies: [], origins: [] } })`) rather than a separate browser launch.

### 7.4 Database reset between journeys

Every journey starts from an identical, known database state.

- Reset **truncates business tables** and re-seeds the single E2E user (plus any baseline
  configuration a journey needs). It must **not** drop the schema or the database: RLS policies and
  the EF migrations-history table must survive, or subsequent journeys would run against an
  unprotected or unmigrated database and still appear to pass.
- Reset runs before each journey file. With `workers: 1` this is safe by construction; under any
  future parallelism it would not be (§7.7).
- Respawn — used at Layer 3 — is deliberately not used here: it is a .NET library with no reach
  from the Playwright process. The reset is SQL executed against the E2E stack's own container.
- Migrations and all RLS scripts are applied once at stack bring-up, not per journey.

Playwright's own preference is start-from-scratch isolation over cleanup-between-tests. A per-test
fresh *database* is too slow for a container-backed stack, so this is a considered compromise:
scratch-equivalent state via truncate-and-reseed, with the persistence layer's own isolation
(RLS + `OwnerUserId`) proven separately at Layer 3.

### 7.5 Determinism, locators, and assertions

- **User-facing locators only**, in Playwright's documented priority order: `getByRole`,
  `getByLabel`, `getByText`, then the remaining user-facing queries. This continues an existing
  convention rather than introducing one — the frontend currently contains **zero** `data-testid`
  attributes, and the Vitest suites already query exclusively by role and label.
- **`data-testid` is not permitted.** If an element cannot be addressed by role, accessible name,
  or label, the defect is the component's accessibility, and the fix belongs in the component —
  which the design-system contract in `CLAUDE.md` already requires. Playwright's docs present test
  IDs as a legitimate option; declining them is a CommitAhead decision, justified by that existing
  accessibility requirement.
- CSS and XPath selectors are not permitted, per Playwright's own guidance that they couple tests
  to DOM structure.
- **Web-first, auto-retrying assertions only** (`toBeVisible`, `toHaveText`, `toHaveValue`, …),
  always awaited. A non-retrying assertion over a value read from the page is a defect. Use
  `expect.poll`/`expect.toPass` where a condition genuinely needs polling.
- **`waitForTimeout` is banned outright** in committed tests. Playwright's auto-waiting and
  retrying assertions make fixed sleeps unnecessary; a sleep that appears to fix a test is hiding a
  real race. (Playwright documents `waitForTimeout` as a debugging aid; the outright ban here is a
  CommitAhead rule, not a quotation of the official docs.)
- **The SPA has no URL routing.** `App.tsx` navigates through `useState<View>`, so there are no
  per-page URLs. Journeys must `page.goto('/')` once and then navigate by interacting with the UI.
  Deep-linking, `toHaveURL` assertions on view changes, and "go straight to the detail page" setup
  shortcuts are all unavailable — a constraint to design around, not to work around.

### 7.6 Zero real external calls

No E2E run may make a real call to Supabase Auth, Supabase Storage, or any AI provider. This is the
same absolute rule as the rest of the suite, applied to a stack that — unlike
`WebApplicationFactory` — has real network access.

- **AI.** Two facts constrain the design: `FakeAIProvider` exists only in test assemblies and is
  not reachable from the production image, and `AnthropicAIProvider`'s base address is currently
  **hardcoded** to `https://api.anthropic.com/`. The preferred resolution is to make that base
  address configurable and run a deterministic stub AI service inside the E2E Compose stack: it
  keeps every test double out of the production assembly, satisfies architecture rule 5, and
  exercises the real adapter's request construction and deserialisation. The alternative — an
  E2E-gated `IAIProvider` registration swapped in at the composition root — is acceptable only if
  it carries the same fail-closed startup guard as §7.3. Whichever is chosen, the requirement is
  the same: deterministic AI responses, and no egress to a real provider.
- **Enforce it, don't assert it.** The app container must have no route to the public internet in
  the E2E stack (internal Compose network, external egress limited to the stub). "We didn't
  configure a real key" is not evidence; an unroutable network is.
- **Supabase Auth** is never called: §7.3 replaces login entirely, and JWT validation uses the E2E
  key rather than Supabase's JWKS.
- **Supabase Storage** is never called: journey 3 uses pasted text (§7.1).

### 7.7 Execution, parallelism, and CI

- **`workers: 1`, serial, from the start.** Playwright's CI guidance already recommends a single
  worker for stability; here it is also a correctness requirement, because all four journeys share
  one database, one seeded owner, and one truncate-based reset. Note that Playwright's docs
  discourage `test.describe.serial` in favour of isolated tests — that guidance concerns
  interdependent tests within a file, and does not conflict with running independent journey files
  one at a time; each journey must still be independently runnable in isolation.
- **The path to parallelism, when it is wanted:** provision one owner account per worker keyed on
  `parallelIndex` (stable across worker restarts, unlike `workerIndex`), give each its own
  `storageState`, and replace the global truncate with per-owner cleanup. Until that exists,
  raising `workers` above 1 will produce cross-journey interference that looks like flakiness.
  Sharding is out of scope: it is a fix for suites far larger than four journeys.
- **Chromium only for the MVP** (`devices['Desktop Chrome']`). Cross-browser rendering risk is
  covered by the design-system component tests; this is a single-user, invite-only application, and
  a browser matrix would multiply E2E runtime for a risk this project does not carry. Revisit only
  if a real cross-browser defect appears.
- **Retries: 0 locally, 1 in CI.** Local retries hide races from the person who just wrote them;
  one CI retry absorbs genuine infrastructure noise while still surfacing the result as *flaky*
  rather than *passed*. A test that only passes on retry is treated as failing.
- **Artifacts:** `trace: 'on-first-retry'`, `screenshot: 'only-on-failure'`,
  `video: 'retain-on-failure'`. (`screenshot` has no `retain-on-failure` mode; `only-on-failure` is
  the correct literal.)
- **Post-merge or manual only** — E2E is not a blocking PR gate. It needs a full container build
  plus a database bring-up, which would dominate PR feedback time for coverage that Layers 1–6
  already provide. This matches how the CI table in `CLAUDE.md` already classifies it.
- CI installs only what it uses: `npx playwright install --with-deps chromium`. If the run is
  containerised instead, use the official pinned image (`mcr.microsoft.com/playwright:v1.62.0-noble`
  or the then-current version) with `--ipc=host`, whose absence is a documented cause of Chromium
  crashes.

### 7.8 Restraint in fixtures, helpers, and Page Objects

Four journeys do not justify a framework.

- Start with **no Page Objects**. Introduce one only when the same interaction is duplicated across
  at least two journeys and the duplication is actually causing churn. Playwright's docs describe
  POM as *one* way to structure a suite, not a requirement, and explicitly tolerate some
  duplication when it keeps tests readable — for a suite this small, an abstraction layer costs
  more than it saves. (This restraint is a CommitAhead decision; the official POM page takes no
  position against over-abstraction.)
- Where shared setup *is* needed, prefer a **fixture** over `beforeEach` plus module-level state —
  fixtures are what Playwright's docs recommend for setup/teardown pairs and shared helpers, and
  module-level mutable state is exactly what breaks when execution order changes.
- Any Page Object introduced later must be wired through a fixture, per the official composition,
  and must hold locators and interactions only — never assertions about business rules that belong
  in the journey.

### 7.9 API-assisted setup

`APIRequestContext` is used **only to prepare state that is not the behaviour under test**.
Preparing the very thing a journey exists to prove makes the journey vacuous — journey 2 must
create its StudyItem through the UI, journey 4 must edit selections through the UI.

Legitimate: seeding a ProfessionalProfile with canonical entries so journey 4 has something to
select; creating prerequisite records a journey depends on but does not exercise.

Two CommitAhead specifics any setup helper must respect:

- **CSRF.** Every state-changing request needs a token from `GET /auth/csrf` sent back as the
  `X-CSRF-TOKEN` header, exactly as the frontend client does. Setup that skips this will get 400s
  that look like application bugs.
- **Cookie sharing.** `page.request` shares the browser context's cookie jar, so it inherits the
  journey's session; a context created via `apiRequest.newContext()` does not. Use `page.request`
  for setup that should act as the signed-in user.

### 7.10 PDF verification scope

Journey 4 asserts that the export **reaches the user**: a download event fires, the suggested
filename is a `.pdf`, and the downloaded bytes are non-empty and begin with the `%PDF-` magic
number.

It deliberately stops there. Parsed-content assertions — required text, entry ordering, exclusions,
locale dates, page limit — already run on every PR against the real renderer via PdfPig
("PDF and CV Verification" above). Re-asserting them here would mean adding a second, independent
PDF-parsing stack in TypeScript whose disagreements with PdfPig would be noise, not signal.

### 7.11 Sources and project decisions

Official Playwright documentation reviewed 2026-08-12 against release `1.62.x`. Each row lists the
source consulted and what CommitAhead decided in light of it.

| Official source | CommitAhead decision |
|---|---|
| [Isolation / browser contexts](https://playwright.dev/docs/browser-contexts) | Per-test context isolation is accepted as-is; it does not cover our shared database, so §7.4 adds truncate-and-reseed between journeys |
| [Best practices](https://playwright.dev/docs/best-practices) | Adopted: user-facing locators, no CSS/XPath, controlled test data, don't test third parties (§7.5, §7.6) |
| [Authentication](https://playwright.dev/docs/auth) | `storageState` mechanism adopted; UI-based sign-in **rejected** (magic link is external and would be a real Supabase call). Deviation: session re-minted per journey, because our 15-minute `iat` cap staleness-expires a once-per-run state (§7.3) |
| [Locators](https://playwright.dev/docs/locators) | Priority order adopted verbatim; `getByTestId` **declined** — the frontend has zero `data-testid` and accessible names are already a design-system requirement (§7.5) |
| [Assertions](https://playwright.dev/docs/test-assertions) · [Actionability](https://playwright.dev/docs/actionability) | Web-first auto-retrying assertions mandatory; fixed sleeps banned outright, which is stricter than the docs' own framing (§7.5) |
| [Test retries](https://playwright.dev/docs/test-retries) · [Trace viewer](https://playwright.dev/docs/trace-viewer) · [Videos](https://playwright.dev/docs/videos) · [Screenshots](https://playwright.dev/docs/screenshots) | `retries` 0 local / 1 CI (fewer than the generated config's 2); `on-first-retry` trace, `only-on-failure` screenshot, `retain-on-failure` video (§7.7) |
| [Parallelism](https://playwright.dev/docs/test-parallel) · [CI](https://playwright.dev/docs/ci) | `workers: 1` adopted — recommended for CI stability, and required here by shared-database state. Per-worker account isolation documented as the prerequisite for ever raising it (§7.7) |
| [Test projects / dependencies](https://playwright.dev/docs/test-projects#dependencies) | Setup-project dependency model used for auth wiring, subject to the per-journey re-mint in §7.3 |
| [Fixtures](https://playwright.dev/docs/test-fixtures) · [Page object models](https://playwright.dev/docs/pom) | Fixtures preferred over hooks; POMs deferred until duplication justifies them — a project restraint, not an official position (§7.8) |
| [API testing](https://playwright.dev/docs/api-testing) · [APIRequestContext](https://playwright.dev/docs/api/class-apirequestcontext) | Used for out-of-scope state only; CSRF token and `page.request` cookie-sharing behaviour called out as CommitAhead specifics (§7.9) |
| [Downloads](https://playwright.dev/docs/downloads) | Download event + `suggestedFilename()` + magic-byte check only; deep PDF assertions stay with PdfPig (§7.10) |
| [Docker](https://playwright.dev/docs/docker) · [Browsers](https://playwright.dev/docs/browsers) | Chromium-only install; pinned official image with `--ipc=host` if containerised (§7.7) |
| [webServer](https://playwright.dev/docs/test-webserver) | **Not used** — Compose lifecycle is owned by explicit scripts so reset can address the same project and teardown actually runs (§7.2) |

---

## CI Gates Summary

See `CLAUDE.md` for the complete list of blocking PR gates and post-merge gates.
