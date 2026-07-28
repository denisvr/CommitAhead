# CommitAhead — Implementation Roadmap

The roadmap is organised as vertical slices. Every phase must leave a working, tested increment; infrastructure and domain layers are added only when the current slice needs them. A phase starts only after its blocking TBDs are decided and the previous phase's CI gates pass.

---

## Phase 0 — Secure Foundation *(current — Phase 0A and part of the rest of Phase 0 are implemented)*

**Outcome:** An authenticated owner can run a production-shaped empty application shell locally; CI proves the architecture and security baseline.

**Phase 0A is implemented** (`docs/prompts/phase-0a-claude-kickoff.md` — solution skeleton and architecture baseline). A further slice of Phase 0 — everything that doesn't require a live Supabase project — is also implemented. **Auth, CSRF, security headers, and the authenticated screen are not implemented**; they need a real Supabase project, which does not exist yet.

- [x] Create `.slnx` and Domain, Application, Infrastructure, and API projects (`backend/`)
- [x] Create React 19 + Vite + TypeScript project and a frontend component test (`frontend/`) — E2E project not yet added
- [x] Configure project references and the API composition root according to ADR-0013
- [x] Serve the production Vite build from Kestrel on the same origin (copied into the published artifact's `wwwroot` at publish time, not into the backend source tree)
- [x] Wire EF Core + Npgsql; a minimal `User` identity table (id, supabase_user_id, email, is_enabled, created_at_utc — ADR-0015) has a generated `InitialCreate` migration. `commitahead_app` and a separate migration role (`backend/scripts/database/001_roles.sql`, `002_rls_users.sql`) are applied and verified end-to-end against a local Docker Postgres (`backend/docker-compose.yml`) — **not yet against the real Supabase project**, which doesn't exist yet; the same scripts are the template for it
- [ ] Create the Supabase project and apply the migration bundle to it
- [ ] Implement backend-mediated magic-link/PKCE auth, per-user authorization (ADR-0015), refresh/logout, CSRF, and security headers
- [ ] Add a minimal authenticated health/home screen (Phase 0A added only an anonymous health endpoint, deliberately with no auth)
- [x] Add OpenAPI generation (build-time, via `Microsoft.Extensions.ApiDescription.Server`) and generated TypeScript client compilation (`frontend/src/api/generated`)
- [x] Add the five NetArchTest rules (4 active; the repository half of rule 5 is active against `IUserRepository`/`UserRepository`; the `IAIProvider` half is still skipped/pending — see `CLAUDE.md`)
- [x] Add blocking CI: Gitleaks and generated-client drift, on top of the build/format/lint/type-check/test/NuGet+npm audit gates already in place

**Exit criteria:** production builds run locally from Kestrel; unauthenticated/non-owner/CSRF/header tests pass; no frontend Supabase key exists.

---

## Phase 1 — Ranked Study Queue

**Outcome:** The daily preparation loop works end-to-end without AI: create a StudyItem, review it, and see deterministic ranking.

**Decide first:** typed StudyItemDetails persistence, EffectiveScore tiebreaker, and component/UI library.

- [ ] Implement StudyItem, four typed details variants, StudyReview, PriorityOverride, ScoringWeights, and EffectiveScorePolicy
- [ ] Implement ScoringConfig optional override persistence and resolver
- [ ] Implement EvidenceLink target schema required by the full Demand query; no creation command exists yet
- [ ] Add EF mappings, migration, repositories/query ports, and ranked-list SQL
- [ ] Implement Create/Update/Archive/Delete StudyItem, SubmitStudyReview, Set/ClearPriorityOverride, Update/ResetScoringConfig, and GetRankedStudyQueue
- [ ] Add Controllers and OpenAPI contracts
- [ ] Build ranked queue, detail view, typed forms, tag input, review form, and score breakdown UI
- [ ] Add domain, use-case, PostgreSQL, API, and frontend component tests for the slice

**Exit criteria:** E2E Create → Review → Rank passes; deletion guards, mastery recency, Demand clamp, overrides, and deterministic ordering are verified.

---

## Phase 2 — Professional Profile and CV Editing

**Outcome:** Canonical career data can be maintained once and curated into independently editable regional CVPresentations.

- [ ] Implement ProfessionalProfile, ContactInfo, all seven canonical child collections, YearMonth, and skill-reference guards
- [ ] Implement independent CVPresentation aggregate according to ADR-0012
- [ ] Add canonical child tables, Experience/Project skill joins, and seven ordered CV selection tables
- [ ] Implement ProfessionalProfile CRUD; canonical-entry deletion removes affected CV selections and guards referenced Skills
- [ ] Implement Create/Update/Delete/Get CVPresentation, including same-profile selection validation and polymorphic-source cleanup
- [ ] Build ProfessionalProfile editors, CVPresentation selection/reordering, formatting rules, and preview shell
- [ ] Add persistence, use-case, API, and component tests, including selection ordering and FK behavior

**Exit criteria:** a CVPresentation can curate canonical entries without duplicating them; editing one presentation does not mutate another.

---

## Phase 3 — Evidence Sources

**Outcome:** Job descriptions and real interview notes are safely stored and ready to influence preparation.

**Decide first:** PDF extraction library and parser resource limits.

- [ ] Implement JobAnalysis, JobSource, JobRequirement, JobGap, and InterviewNote
- [ ] Add pasted-text and secure PDF-upload flows: validation, private quarantine key, bounded one-time extraction, and failure cleanup
- [ ] Implement Create/Update/Delete JobAnalysis and InterviewNote
- [ ] Apply ADR-0011: source deletion removes EvidenceLinks and AnalysisDrafts transactionally; uploaded-file cleanup is best effort after commit
- [ ] Preserve InterviewNotes when their optional JobAnalysis is deleted (`ON DELETE SET NULL`)
- [ ] Implement DeleteEvidenceLink; creation remains exclusive to accepted LinkProposals
- [ ] Build JobAnalysis and InterviewNote interfaces, including extracted-text verification
- [ ] Add PDF fixtures/failure tests, source-deletion integration tests, and API/component coverage

**Exit criteria:** pasted and PDF job sources plus interview notes are fully manageable; malicious/unsupported PDFs are rejected safely.

---

## Phase 4 — Explicit AI Analysis

**Outcome:** All three explicit AI commands produce reviewable drafts; accepted effects are applied atomically with controlled cost.

**Decide first:** AI provider/model, default budgets/currency, StructuredSuggestion allowlist.

- [ ] Finalise IAIProvider contracts and command-specific minimised input projections
- [ ] Implement FakeAIProvider with six deterministic scenarios per command
- [ ] Implement ProviderAIAdapter with structured output, time/token limits, and safe error mapping
- [ ] Implement AnalysisDraft and immutable proposed/separate accepted proposal payload persistence
- [ ] Implement AIUsageRecord Reserved → Completed/Failed lifecycle, durable idempotency, lazy stale-reservation reconciliation, and budget checks
- [ ] Implement AnalyzeJobAnalysis, AnalyzeCVPresentation, and AnalyzeInterviewNote
- [ ] Implement ApplyAnalysisDraft with exactly one decision per proposal and one atomic accepted-effects transaction
- [ ] Extend all evidence-source deletion use cases/tests to remove AnalysisDrafts and proposal children according to ADR-0011
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
