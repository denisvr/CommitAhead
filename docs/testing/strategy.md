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
| E2E | Playwright (planned — not implemented; see Layer 7) |
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
- An encrypted PDF (a real PDF standard security handler encryption dictionary — RC4 40-bit, Revision 2 — hand-computed with a genuine non-empty user password) → PdfPig, opened without that password, fails authentication and throws for real (`PdfPigTextExtractorTests`); not a claim resting on the source's catch clause alone
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

**Not implemented yet** — no Playwright project exists in this repo. Deferred until there is a real deployed environment to point it at (rather than building the test-environment auth scheme and CI bootstrap this layer needs against a purely local target); revisit then. The plan below is the target shape once that work starts, not a description of anything running today.

Four critical journeys. Environment: production Vite build + real Kestrel + Testcontainers PostgreSQL + `FakeAIProvider` + test-environment auth scheme (also not implemented yet — nothing today lets a real running Kestrel process accept anything but a genuine Supabase session; this needs new, environment-gated auth surface in the API itself, not just test-project wiring). DB reset between journeys. No Supabase, no real AI.

1. Authenticated access
2. Create StudyItem → SubmitStudyReview → verify ranking
3. Complete job-analysis draft flow
4. Edit CVPresentation + export

---

## CI Gates Summary

See `CLAUDE.md` for the complete list of blocking PR gates and post-merge gates.
