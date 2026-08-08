# CommitAhead — Implementation Roadmap

The roadmap is organised as vertical slices. Every phase must leave a working, tested increment; infrastructure and domain layers are added only when the current slice needs them. A phase starts only after its blocking TBDs are decided and the previous phase's CI gates pass.

---

## Phase 0 — Secure Foundation *(security/architecture baseline complete; E2E and one architecture rule fragment remain pending)*

**Outcome:** An authenticated, enabled user can run a production-shaped empty application shell locally; CI proves the architecture and security baseline.

The Supabase project exists (`Devalente Org` / `CommitAhead`, West EU/Ireland) and is used for Auth only right now — `Supabase:Url`/`Supabase:AnonKey` point at it, while `ConnectionStrings:CommitAheadDb` still points at the local Docker Postgres (`backend/docker-compose.yml`), which already has an enabled user's `users` row seeded. This is a deliberate, accepted split: development uses Supabase Auth (remote) for authentication while the application's own PostgreSQL stays entirely local via Docker — there is no need to develop against the real Postgres before deployment. Backend-mediated magic-link/PKCE auth, per-user authorization, CSRF, and security headers are implemented and verified end-to-end this way — including one real call to the live Supabase Auth API (`POST /auth/v1/otp` → `200`).

**Applying the migration bundle and roles/RLS scripts to the real Supabase Postgres is deferred to deployment (Phase 6)**, not blocking Phase 0 — it needs the project's real database password, which stays with the user (see `README.md`).

- [x] Create `.slnx` and Domain, Application, Infrastructure, and API projects (`backend/`)
- [x] Create React 19 + Vite + TypeScript project and a frontend component test (`frontend/`)
- [ ] *(Pending — not yet started)* Add the E2E (Playwright) project and its four journeys
- [x] Configure project references and the API composition root according to ADR-0013
- [x] Serve the production Vite build from Kestrel on the same origin (copied into the published artifact's `wwwroot` at publish time, not into the backend source tree)
- [x] Wire EF Core + Npgsql; a minimal `User` identity table (id, supabase_user_id, email, is_enabled, created_at_utc — ADR-0015) has a generated `InitialCreate` migration plus a follow-up migration adding a case-insensitive unique index on email. `commitahead_app` and a separate migration role (`backend/scripts/database/001_roles.sql`, `002_rls_users.sql`) are applied and verified end-to-end against a local Docker Postgres (`backend/docker-compose.yml`), via the reproducible `backend/scripts/setup-local-db.ps1` (roles → migrations → RLS in one command; `002_rls_users.sql` is idempotent)
- [x] Create the Supabase project
- [ ] *(Deferred to Phase 6/deployment, not blocking)* Apply the migration bundle and `001_roles.sql`/`002_rls_users.sql`/`003_rls_phase1.sql`/`004_rls_phase2.sql` to the real Supabase Postgres, and seed each enabled user's `users` row there — user-run, needs the real DB password
- [x] Implement backend-mediated magic-link/PKCE auth (`/auth/login`, `/auth/callback`, `/auth/refresh`, `/auth/logout`), per-user authorization (ADR-0015), CSRF (`/auth/csrf` + validation middleware), and security headers
- [x] Closed login: `/auth/login` normalizes and validates the email, looks up a provisioned + enabled `User` (case-insensitive unique index), and only calls Supabase for a match — an unknown or disabled email gets the same generic response without ever reaching Supabase
- [x] Secure-by-default authorization: an `AuthorizationOptions` fallback (and default) policy requires authentication plus an enabled ADR-0015 user for every endpoint; only Health and the `/auth/*` endpoints carry `[AllowAnonymous]`
- [x] The ADR-0015 enabled-user check is an authorization policy/handler applied via that fallback policy, not global middleware — it never blocks `/auth/login`, `/auth/callback`, `/auth/refresh`, `/auth/logout`, or `/auth/csrf`, and logout always clears cookies even if the external Supabase revoke call fails
- [x] Session hardening: the frontend does a single-flight refresh-and-retry on 401 (get CSRF → `/auth/refresh` → retry the original request once); the backend enforces an effective 15-minute access-token limit independent of the token's own `exp` (via its `iat` claim); the session-start timestamp is sealed with ASP.NET Data Protection so the 7-day absolute timeout holds even against a non-browser replay of a captured cookie; the frontend attempts a refresh before logout so `/auth/logout` has a live token to revoke
- [x] The SPA fallback route no longer swallows unmatched `/api` or `/auth` requests into `index.html` — they return a real 404
- [x] Add a minimal authenticated home screen (`GET /api/me` + a login form / signed-in view in `frontend/`)
- [x] Add OpenAPI generation (build-time, via `Microsoft.Extensions.ApiDescription.Server`) and generated TypeScript client compilation (`frontend/src/api/generated`)
- [x] Add the five NetArchTest architectural rules (4 fully active; rule 4 — controllers depend on Application only — is enforced by two tests, including one preventing controllers from injecting repositories directly; the repository half of rule 5 is active against `IUserRepository`/`UserRepository`; the `IAIProvider` half of rule 5 remains skipped/pending until Phase 4 declares that interface — see `CLAUDE.md`)
- [x] Add blocking CI: Gitleaks and generated-client drift, on top of the build/format/lint/type-check/test/NuGet+npm audit gates already in place; `dotnet publish` now fails (not just warns) when `frontend/dist` is missing, and a dedicated CI job builds the frontend, publishes the backend, starts the published Kestrel app, and verifies `/`, `/api/health`, and that unmatched `/api`/`/auth` routes 404

**Exit criteria:** production builds run locally from Kestrel; unauthenticated/unknown-or-disabled-user/CSRF/header tests pass (locally-signed JWTs, per `docs/testing/strategy.md`); no frontend Supabase key exists. E2E and the `IAIProvider` architecture-rule fragment remain open and are not exit-criteria for Phase 0 as scoped here — they are tracked above as pending.

---

## Phase 1 — Ranked Study Queue *(implementation complete; E2E exit criterion pending — deferred until there is a real deployed environment to run Playwright against)*

**Outcome:** The daily preparation loop works end-to-end without AI: create a StudyItem, review it, and see deterministic ranking.

Typed StudyItemDetails persistence (single `jsonb` column, self-describing discriminator) and the
ranked-queue tiebreaker (`EffectiveScore DESC, CreatedAt ASC, Id ASC`) are decided — see
`docs/architecture/persistence.md`. The frontend component/styling decision is closed by ADR-0016.

- [x] Implement StudyItem, four typed details variants, StudyReview, PriorityOverride, ScoringWeights, and EffectiveScorePolicy
- [x] Implement ScoringConfig optional override persistence and resolver
- [x] Implement EvidenceLink target schema required by the full Demand query; no creation command exists yet
- [x] Add EF mappings, migration, repositories/query ports, and the ranked-queue query (loads the owner's small, owner-scoped Active-item set and ranks it in memory with the same `EffectiveScorePolicy` the domain uses — not a SQL-level `ORDER BY` on a computed score; see `docs/architecture/persistence.md`)
- [x] Implement Create/Update/Archive/Delete StudyItem, SubmitStudyReview, Set/ClearPriorityOverride, Update/ResetScoringConfig, and GetRankedStudyQueue
- [x] Add Controllers and OpenAPI contracts
- [x] Port the approved Reading Room tokens/assets into `frontend/src/design-system/` and implement
      only the production primitives required by this slice
- [x] Build ranked queue, detail view, typed forms, tag input, review form, and score breakdown UI
- [x] Add domain, use-case, PostgreSQL, API, and frontend component tests for the slice

**Exit criteria:** E2E Create → Review → Rank passes; deletion guards, mastery recency, Demand clamp, overrides, and deterministic ordering are verified. The non-E2E half is verified today by Layers 1–4 (domain/use-case/repository/API tests, including real-runtime-role RLS integration tests) and Layer 6 (frontend component tests, MSW-backed) — see `docs/testing/strategy.md`. The Playwright journey itself has not been written; Phase 1 is not marked complete until it exists and passes.

---

## Phase 2 — Professional Profile and CV Editing *(implementation complete; E2E exit criterion pending, same accepted gap as Phase 0/1)*

**Outcome:** Canonical career data can be maintained once and curated into independently editable regional CVPresentations.

- [x] Implement ProfessionalProfile, ContactInfo, all seven canonical child collections, YearMonth, and skill-reference guards
- [x] Implement independent CVPresentation aggregate according to ADR-0012
- [x] Add canonical child tables, with skill references and CVPresentation's seven selections mapped as plain `uuid[]` array columns rather than the originally-planned FK-backed join tables (**deviation flagged, not silent — see ADR-0017**: an EF Core constructor-binding limitation, not a preference; the domain aggregate already fully enforces every invariant those join tables would have backed up at the DB level: skill references must exist, a referenced Skill can't be deleted, selection order is positional by construction). `CVPresentation` carries a composite FK on `(ProfessionalProfileId, OwnerUserId)` against a matching alternate key on `ProfessionalProfile`, making a cross-owner reference (invariant 29) impossible to persist, independent of the application-level check
- [x] Implement ProfessionalProfile CRUD; canonical-entry deletion removes affected CV selections (`DanglingSelectionCleanup`, run from every `Replace*UseCase`) and guards referenced Skills
- [x] Implement Create/Update/Delete/Get CVPresentation, including same-profile selection validation (invariant 23) and locale validation (an unrecognized `locale` is rejected at the domain level against the runtime's own culture list). Polymorphic-source cleanup (ADR-0011) is explicitly Phase 4 work, not this phase's — CVPresentation cannot yet be an EvidenceLink/AnalysisDraft source
- [x] Grants and RLS for all nine Phase 2 tables (`004_rls_phase2.sql`): `professional_profiles`/`cv_presentations` scoped directly by `owner_user_id`, the seven canonical child tables scoped transitively through `professional_profile_id`, mirroring `003_rls_phase1.sql`'s `study_reviews` pattern
- [x] Build ProfessionalProfile editors, CVPresentation selection/reordering, and preview shell. **Formatting rules, scoped down deliberately:** the preview renders locale-aware month/year via `Intl.DateTimeFormat` (the substantive part of "formatting rules"), with a plain `YYYY-MM` fallback if a persisted locale predates the backend's own validation or isn't one `Intl.DateTimeFormat` recognizes; a presentation's free-text `dateFormat` pattern (e.g. `"dd MMM yyyy"`) is not parsed/applied literally — `YearMonth` has no day component, so a general date-pattern engine is disproportionate to this slice
- [x] Add persistence, use-case, API, and component tests, including selection ordering and FK behavior — covering RLS isolation for the Phase 2 tables (two-user isolation, denial without owner context, safe script reapplication), the CVPresentation→ProfessionalProfile cross-owner FK rejection, locale validation, and the selection-save race fix (functional state updates so a slower section's completion can't overwrite a faster sibling's). Component tests cover every new page's load/error states and the two new generic design-system components (`EntryListEditor`, `SelectionOrderEditor`); a live authenticated click-through was not possible in the dev sandbox used to build this phase (no way to complete the real Supabase magic-link flow there) — recommended as a manual pass before considering the phase fully verified end-to-end

**Exit criteria:** a CVPresentation can curate canonical entries without duplicating them; editing one presentation does not mutate another. The non-E2E half is verified by Layers 1–4 (domain/use-case/repository/API tests) and Layer 6 (frontend component tests, MSW-backed) — see `docs/testing/strategy.md`. The Playwright journey itself has not been written; Phase 2 is not marked complete until it exists and passes, matching Phase 0/1's own accepted gap.

---

## Phase 3 — Evidence Sources

**Outcome:** Job descriptions and real interview notes are safely stored and ready to influence preparation.

**Decide first:** ~~PDF extraction library and parser resource limits~~ — decided: PdfPig 0.1.15, 5 MB/20-page/50,000-character limits, 10-second best-effort timeout (see `docs/tbd.md`'s former entry, now resolved; ADR-0010, ADR-0018).

- [x] Implement JobAnalysis, JobSource, JobRequirement, JobGap, and InterviewNote
- [x] Add pasted-text and secure PDF-upload flows: validation, private quarantine key, bounded one-time extraction, and failure cleanup (`CreateJobAnalysisFromUploadUseCase`, `PdfPigTextExtractor`, `SupabaseStorageClient`; `POST /api/job-analyses/upload`). Bucket/RLS provisioning (`006_storage_job_postings.sql`) is a one-time operator action against the real Supabase project, deferred to deployment (Phase 6) — not applied locally or in CI
- [x] Implement Create/Update/Delete JobAnalysis and InterviewNote (Application use cases, EF repositories/migration, and `JobAnalysesController`/`InterviewNotesController`)
- [x] Apply the uploaded-file-cleanup half of ADR-0011 only: after a JobAnalysis deletion commits, best-effort delete its `UploadedFile`'s Storage object (`DeleteJobAnalysisUseCase`). The EvidenceLink/AnalysisDraft transactional-cleanup half of ADR-0011 moves to Phase 4 (see below) — neither aggregate has a creation path yet, so there is nothing for a Phase 3 deletion use case to clean up
- [x] Preserve InterviewNotes when their optional JobAnalysis is deleted (`ON DELETE SET NULL`) — a real PostgreSQL FK (`InterviewNoteConfiguration`), verified with a real-Postgres integration test (`InterviewNoteRepositoryTests.DeletingTheReferencedJobAnalysis_NullsTheNotesReference_AndPreservesTheNote`), not application code
- [x] Build JobAnalysis and InterviewNote interfaces, including extracted-text verification — `job-analyses`/`interview-notes` features: list/create/detail pages, paste-or-upload create form, and the uploaded-PDF's extracted text shown for verification on the detail page
- [x] Add PDF fixtures/failure tests, source-deletion integration tests, and API/component coverage — PDF fixtures/failure tests (`PdfPigTextExtractorTests`: Malformed/ImageOnly/TooManyPages/TooMuchText/Encrypted against the real extractor, the last against a small, real, password-protected PDF committed as a binary fixture — `JobAnalyses/Fixtures/encrypted.pdf` — plus a multi-page test proving adjacent pages' words are never merged), source-deletion integration tests, API coverage (`JobAnalysesEndpointTests` upload round-trip), and frontend component coverage all exist. Not covered: a deterministic real-PdfPig `TimedOut` fixture — impractical without a pathological file; covered only at the Application-test level via a fake extractor, which says nothing about PdfPig's real timing
- [x] Corrective pass: log the orphaned Storage object's key (never the exception) on every Storage-cleanup failure; enforce JobGap's RequirementId-belongs-to-the-same-JobAnalysis invariant with a real PostgreSQL composite foreign key, not just in-memory, as defense-in-depth (verified the composite FK's `Restrict` delete behavior does not block a real JobAnalysis deletion that removes both a JobRequirement and its JobGap); narrow `PdfPigTextExtractor`'s exception handling to the two known PdfPig failure types (anything else now propagates as a genuine infrastructure error); join extracted pages with a newline and normalize line endings; fix `InterviewNoteForm`'s default date to the browser's local calendar date instead of UTC
- [x] Final cleanup: split `CreateJobAnalysisUseCase` into `CreateJobAnalysisFromPastedTextUseCase`, which takes the posting text as a plain string and constructs `PastedText` itself rather than accepting a generic `JobSource` — the pasted-text/upload trust boundary (only `CreateJobAnalysisFromUploadUseCase` may construct an `UploadedFile`) is now a type-level fact, not just a doc-comment convention

**Exit criteria:** pasted and PDF job sources plus interview notes are fully manageable; malicious/unsupported PDFs are rejected safely.

---

## Phase 4 — Explicit AI Analysis

**Outcome:** All three explicit AI commands produce reviewable drafts; accepted effects are applied atomically with controlled cost.

**Decide first:** AI provider/model, default budgets/currency, ~~StructuredSuggestion allowlist~~ — decided (docs/tbd.md). Real provider/model selection is deferred by explicit user decision — Slices 1-2 below cover only what needs no real provider.

- [x] Finalise IAIProvider contracts and command-specific minimised input projections — `IAIProvider` + `JobAnalysisAiInput`/`CVPresentationAiInput`/`InterviewNoteAiInput`/`StudyItemCatalogueEntry`/`AiAnalysisResult` (`backend/src/CommitAhead.Application/AI/`)
- [x] Implement FakeAIProvider with six deterministic scenarios per command — `FakeAIProvider`/`FakeAIScenario` (`backend/tests/CommitAhead.Application.Tests/AI/`); same six scenarios apply uniformly across all three analyze methods
- [ ] Implement ProviderAIAdapter with structured output, time/token limits, and safe error mapping — blocked on real provider/model selection
- [x] Implement AnalysisDraft and immutable proposed/separate accepted proposal payload persistence — Domain aggregate (Slice 1) plus EF persistence (Slice 3): JSONB-mapped proposal payloads (`SuggestionPayloadValueConverter`, reused `StudyItemDetailsValueConverter`), `AnalysisDraftRepository`, migration, and a database-level partial unique index enforcing at most one Pending draft per source (`AnalysisDraftConfiguration`, verified by a test that bypasses the use-case-level check)
- [x] Implement AIUsageRecord Reserved → Completed/Failed lifecycle and durable idempotency — Domain aggregate (Slice 1) plus EF persistence (Slice 3, revised in Slice 4): `AIUsageRecordRepository`, a real unique database constraint scoped to `(owner_user_id, idempotency_key)` — not `idempotency_key` alone, so the same string is independently reusable by different owners (ADR-0015) — plus a per-owner partial-unique index allowing at most one `Reserved` record per owner at a time (the "one AI call in flight" lock is per owner, never system-wide), and `GetSpentCostAsync` for the (still unenforced) budget check. Lazy stale-reservation reconciliation (ADR-0014) is now implemented, inline in `AnalyzeJobAnalysisUseCase`'s reservation transaction — no background worker
- [x] Implement AnalyzeJobAnalysis, AnalyzeCVPresentation, and AnalyzeInterviewNote — `AnalyzeJobAnalysisUseCase`/`AnalyzeCVPresentationUseCase`/`AnalyzeInterviewNoteUseCase` (`backend/src/CommitAhead.Application/AI/`), sharing one extracted `AnalysisCommandOrchestrator` for the reservation/concurrency/transaction lifecycle every command needs identically (owner-scoped idempotency/concurrency per ADR-0015, inline lazy stale-reservation reconciliation, an atomic draft-persistence-plus-usage-completion transaction via a minimal `IUnitOfWork`) — extracted once all three needed it, so a correctness fix lands once, not three times. Each use case supplies only its own source fetch, minimised input, and StructuredSuggestion allowlist validation: AnalyzeJobAnalysis's `AddJobRequirement`/`AddJobGap` pair uses a same-response reference mechanism so one pass can propose both without ever trusting an AI-generated Guid as a real database Id (`AiStructuredSuggestionValidator`); AnalyzeCVPresentation's `UpdateCVPresentationSummary` and AnalyzeInterviewNote's `AddInterviewGap`/`AddInterviewLesson` are self-contained, single-field commands needing no such mechanism (`AiSimpleSuggestionValidator`). None of the three is registered in the composition root yet — no controller calls them, and no real `IAIProvider` implementation exists, so registering them would fail ASP.NET Core's own DI validation on every application start
- [x] Implement ApplyAnalysisDraft with exactly one decision per proposal and one atomic accepted-effects transaction, and EvidenceLink creation via accepted LinkProposals — `ApplyAnalysisDraftUseCase` (`backend/src/CommitAhead.Application/AnalysisDrafts/`), a new `IEvidenceLinkRepository` (`backend/src/CommitAhead.Application/EvidenceLinks/`, the first real EvidenceLink creation path — no migration needed, the table/RLS already existed from Phase 1). Row-locks the draft (`GetByIdForUpdateAsync`, a real `SELECT ... FOR UPDATE`) so two concurrent applies of the same draft can't both succeed; validates each proposal kind's decisions independently; resolves every decision into an effect before mutating anything; re-validates source/command compatibility instead of trusting AnalyzeX; rejects an EvidenceLink whose target no longer exists or that would duplicate an existing one (checked before mutating, with the database's own unique index as the last-resort concurrent-duplicate guard); every accepted payload is built from the actually-constructed/mutated Domain object, never the caller's raw decision. `DeleteEvidenceLinkUseCase` landed in the next slice, below
- [x] Extend all evidence-source deletion use cases to remove EvidenceLinks and AnalysisDrafts (with their proposal children, via the database's own cascade) transactionally, per ADR-0011 (`DeleteJobAnalysisUseCase`/`DeleteCVPresentationUseCase`/`DeleteInterviewNoteUseCase`, using `IEvidenceLinkRepository.DeleteAllForSourceAsync`/`IAnalysisDraftRepository.DeleteAllForSourceAsync` inside `IUnitOfWork.ExecuteInTransactionAsync`). Also added the standalone `DeleteEvidenceLinkUseCase`. This surfaced and fixed a real bug: `EfUnitOfWork.ExecuteInTransactionAsync` unconditionally began a new transaction, which crashed under `RlsTransactionActionFilter`'s own owner-scoped transaction already wrapping every `[UsesOwnerScopedData]` controller action — it now nests inside an already-active transaction instead of starting a second one on the same connection
- [ ] Build draft review UI with editable final accepted payloads
- [ ] Add adapter tests with stubbed HTTP, use-case/API scenarios, integration atomicity tests, and frontend tests

**Exit criteria:** E2E Job Analysis Draft passes with FakeAIProvider; duplicate requests cannot duplicate charges; automated CI performs zero real AI calls.

---

## Phase 5 — CV Export

**Outcome:** At least one regional CV template produces a verified downloadable document.

**Decide first:** export format/engine.

- [ ] Implement export renderer abstraction and one template
- [ ] Apply restricted Markdown rendering with a runtime-appropriate allowlist sanitizer
- [ ] Apply locale dates, visibility rules, selected-entry order, and page limit
- [ ] Add ExportCVPresentation use case/controller and download UI
- [ ] Add parsed-output assertions on every PR
- [ ] Add one deterministic visual-regression fixture per template post-merge

**Exit criteria:** E2E Edit → Export CV passes; parsed output proves required text, exclusions, ordering, locale, and page limit.

---

## Phase 6 — Production Hardening

**Outcome:** The complete MVP is safely deployable to the internet.

**Decide first:** hosting/secrets platform, Data Protection key storage, backup retention/restore cadence, and log retention.

- [ ] Build reviewed EF migration bundle and production container
- [ ] Configure durable encrypted Data Protection keys and hosting secrets
- [ ] Configure Dependabot for NuGet, npm, Docker, and GitHub Actions
- [ ] Pin Actions to SHAs and minimise workflow permissions
- [ ] Generate SBOM and block deployment on high/critical Trivy findings
- [ ] Run OWASP ZAP baseline against staging with FakeAIProvider
- [ ] Configure encrypted backups and complete a restoration test
- [ ] Run all four Playwright journeys post-merge
- [ ] Add manual live-AI smoke workflow with explicit provider/model/token/cost limits
- [ ] Complete the pre-internet-deployment security checklist

**Exit criteria:** every MVP completion criterion in `docs/product/brief.md` is met and the production deployment passes its security gates.
